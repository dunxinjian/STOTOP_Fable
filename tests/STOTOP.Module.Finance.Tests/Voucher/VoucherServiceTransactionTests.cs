using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Interfaces;
using STOTOP.Core.Services;
using STOTOP.Infrastructure.Data;
using STOTOP.Infrastructure.Repositories;
using STOTOP.Module.Finance.Dtos;
using STOTOP.Module.Finance.Entities;
using STOTOP.Module.Finance.Services;
using Xunit;

namespace STOTOP.Module.Finance.Tests.Voucher;

public class VoucherServiceTransactionTests
{
    private const long Org = 100;
    private const long AcctSet = 7;

    // 包装真实分录仓储，在第 N 次 AddAsync 抛错，模拟"分录写入中途失败"
    private sealed class ThrowOnNthEntryRepository : IRepository<FinVoucherEntry>
    {
        private readonly IRepository<FinVoucherEntry> _inner;
        private readonly int _throwOn;
        private int _count;
        public ThrowOnNthEntryRepository(IRepository<FinVoucherEntry> inner, int throwOn) { _inner = inner; _throwOn = throwOn; }
        public Task<FinVoucherEntry> AddAsync(FinVoucherEntry entity)
        {
            if (++_count == _throwOn) throw new InvalidOperationException("模拟分录写入失败");
            return _inner.AddAsync(entity);
        }
        public Task<FinVoucherEntry?> GetByIdAsync(long id) => _inner.GetByIdAsync(id);
        public Task<List<FinVoucherEntry>> GetAllAsync() => _inner.GetAllAsync();
        public Task UpdateAsync(FinVoucherEntry entity) => _inner.UpdateAsync(entity);
        public Task DeleteAsync(long id) => _inner.DeleteAsync(id);
        public IQueryable<FinVoucherEntry> Query() => _inner.Query();
    }

    private sealed class SqliteOrgAccessor(long orgId) : IOrgContextAccessor
    {
        public long? CurrentOrgId { get; set; } = orgId;
    }

    private static STOTOPDbContext CreateSqliteDb(SqliteConnection conn)
    {
        STOTOPDbContext.RegisterModuleAssembly(typeof(FinAmoebaPLTemplate).Assembly);
        var options = new DbContextOptionsBuilder<STOTOPDbContext>()
            .UseSqlite(conn)
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCustomizer, SqliteCompatModelCustomizer>()
            .Options;
        var db = new STOTOPDbContext(options, new SqliteOrgAccessor(Org));
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Create_rolls_back_header_when_entry_write_fails()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        await using var db = CreateSqliteDb(conn);

        db.Set<FinAccount>().AddRange(
            VoucherServiceTestHarness.Account(1, "1001", "库存现金", AcctSet, Org),
            VoucherServiceTestHarness.Account(2, "3001", "实收资本", AcctSet, Org));
        db.Set<FinAccountPeriod>().Add(VoucherServiceTestHarness.Period(11, 2026, 6, AcctSet));
        await db.SaveChangesAsync();

        var http = VoucherServiceTestHarness.HttpContext(Org, AcctSet);
        var opLog = new OperationLogService(
            new Repository<FinOperationLog>(db), http, Microsoft.Extensions.Logging.Abstractions.NullLogger<OperationLogService>.Instance);
        var changeTracking = new ChangeTrackingService(new Repository<FinChangeHistory>(db), http);
        var service = new VoucherService(
            new Repository<FinVoucher>(db),
            new ThrowOnNthEntryRepository(new Repository<FinVoucherEntry>(db), throwOn: 2),
            new Repository<FinAccount>(db),
            new Repository<FinAccountPeriod>(db),
            opLog, changeTracking, http,
            new VoucherServiceTestHarness.NoOpEventDispatcher(), db);

        var request = new CreateVoucherRequest
        {
            VoucherWord = "记", Date = new DateTime(2026, 6, 15), PeriodId = 0,
            Entries =
            {
                new CreateVoucherEntryRequest { LineNo = 1, Summary = "t", AccountId = 1, DebitAmount = 100m },
                new CreateVoucherEntryRequest { LineNo = 2, Summary = "t", AccountId = 2, CreditAmount = 100m },
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, "tester", AcctSet));

        // 关键断言：分录写入失败后，凭证头不得残留（事务整体回滚）
        Assert.Empty(db.Set<FinVoucher>());
        Assert.Empty(db.Set<FinVoucherEntry>());
    }
}
