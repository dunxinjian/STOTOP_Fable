using STOTOP.Module.Finance.Dtos;
using STOTOP.Module.Finance.Entities;
using Xunit;

namespace STOTOP.Module.Finance.Tests.Voucher;

public class VoucherServicePeriodTests
{
    private const long Org = 100;
    private const long AcctSet = 7;

    private static CreateVoucherRequest BalancedRequest(DateTime date) => new()
    {
        VoucherWord = "记",
        Date = date,
        PeriodId = 0, // 模拟前端不传期间
        Entries =
        {
            new CreateVoucherEntryRequest { LineNo = 1, Summary = "测试", AccountId = 1, DebitAmount = 100m },
            new CreateVoucherEntryRequest { LineNo = 2, Summary = "测试", AccountId = 2, CreditAmount = 100m },
        }
    };

    private static async Task SeedAsync(STOTOP.Infrastructure.Data.STOTOPDbContext db, int periodNo, int isClosed = 0)
    {
        db.Set<FinAccount>().AddRange(
            VoucherServiceTestHarness.Account(1, "1001", "库存现金", AcctSet, Org),
            VoucherServiceTestHarness.Account(2, "3001", "实收资本", AcctSet, Org));
        db.Set<FinAccountPeriod>().Add(
            VoucherServiceTestHarness.Period(11, 2026, periodNo, AcctSet, isClosed));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Create_resolves_period_and_account_set_from_date_when_periodId_zero()
    {
        await using var db = TestDbContextFactory.Create(nameof(Create_resolves_period_and_account_set_from_date_when_periodId_zero), Org);
        await SeedAsync(db, periodNo: 6);
        var http = VoucherServiceTestHarness.HttpContext(Org, AcctSet);
        var service = VoucherServiceTestHarness.Build(db, http);

        var dto = await service.CreateAsync(BalancedRequest(new DateTime(2026, 6, 15)), "tester", AcctSet);

        var saved = db.Set<FinVoucher>().Single(v => v.FID == dto.Id);
        Assert.Equal(11, saved.FPeriodId);
        Assert.Equal(AcctSet, saved.FAccountSetId);
        Assert.Equal(Org, saved.FOrgId);
    }

    [Fact]
    public async Task Create_rejects_when_no_period_for_date()
    {
        await using var db = TestDbContextFactory.Create(nameof(Create_rejects_when_no_period_for_date), Org);
        await SeedAsync(db, periodNo: 6);
        var http = VoucherServiceTestHarness.HttpContext(Org, AcctSet);
        var service = VoucherServiceTestHarness.Build(db, http);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(BalancedRequest(new DateTime(2026, 7, 1)), "tester", AcctSet));
        Assert.Contains("未找到", ex.Message);
        Assert.Empty(db.Set<FinVoucher>());
    }

    [Fact]
    public async Task Create_rejects_when_target_period_closed()
    {
        await using var db = TestDbContextFactory.Create(nameof(Create_rejects_when_target_period_closed), Org);
        await SeedAsync(db, periodNo: 6, isClosed: 1);
        var http = VoucherServiceTestHarness.HttpContext(Org, AcctSet);
        var service = VoucherServiceTestHarness.Build(db, http);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(BalancedRequest(new DateTime(2026, 6, 15)), "tester", AcctSet));
        Assert.Contains("已结账", ex.Message);
        Assert.Empty(db.Set<FinVoucher>());
    }
}
