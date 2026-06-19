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

    // 调用方直接传入已结账期间的 periodId（如桥接/导入路径）也必须被拒，不只是日期解析路径。
    [Fact]
    public async Task Create_rejects_when_explicit_periodId_refers_to_closed_period()
    {
        await using var db = TestDbContextFactory.Create(nameof(Create_rejects_when_explicit_periodId_refers_to_closed_period), Org);
        await SeedAsync(db, periodNo: 6, isClosed: 1); // 期间 FID=11 已结账
        var http = VoucherServiceTestHarness.HttpContext(Org, AcctSet);
        var service = VoucherServiceTestHarness.Build(db, http);

        var request = BalancedRequest(new DateTime(2026, 6, 15));
        request.PeriodId = 11; // 显式传入已结账期间，绕过日期解析分支

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request, "tester", AcctSet));
        Assert.Contains("已结账", ex.Message);
        Assert.Empty(db.Set<FinVoucher>());
    }

    [Fact]
    public async Task Update_reresolves_period_and_tags_entries_with_org()
    {
        await using var db = TestDbContextFactory.Create(nameof(Update_reresolves_period_and_tags_entries_with_org), Org);
        db.Set<FinAccount>().AddRange(
            VoucherServiceTestHarness.Account(1, "1001", "库存现金", AcctSet, Org),
            VoucherServiceTestHarness.Account(2, "3001", "实收资本", AcctSet, Org));
        db.Set<FinAccountPeriod>().AddRange(
            VoucherServiceTestHarness.Period(10, 2026, 5, AcctSet),
            VoucherServiceTestHarness.Period(11, 2026, 6, AcctSet));
        await db.SaveChangesAsync();
        var http = VoucherServiceTestHarness.HttpContext(Org, AcctSet);
        var service = VoucherServiceTestHarness.Build(db, http);

        var created = await service.CreateAsync(BalancedRequest(new DateTime(2026, 5, 10)), "tester", AcctSet);
        Assert.Equal(10, db.Set<FinVoucher>().Single(v => v.FID == created.Id).FPeriodId);

        var updateReq = BalancedRequest(new DateTime(2026, 6, 20));
        await service.UpdateAsync(created.Id, updateReq, "modifier");

        var saved = db.Set<FinVoucher>().Single(v => v.FID == created.Id);
        Assert.Equal(11, saved.FPeriodId);
        var entries = db.Set<FinVoucherEntry>().Where(e => e.FVoucherId == created.Id).ToList();
        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.Equal(Org, e.FOrgId));
    }

    [Fact]
    public async Task SaveDraft_resolves_period_from_date()
    {
        await using var db = TestDbContextFactory.Create(nameof(SaveDraft_resolves_period_from_date), Org);
        await SeedAsync(db, periodNo: 6);
        var http = VoucherServiceTestHarness.HttpContext(Org, AcctSet);
        var service = VoucherServiceTestHarness.Build(db, http);

        var dto = await service.SaveDraftAsync(BalancedRequest(new DateTime(2026, 6, 15)), "tester", AcctSet);

        var saved = db.Set<FinVoucher>().Single(v => v.FID == dto.Id);
        Assert.Equal(11, saved.FPeriodId);
        Assert.Equal(0, saved.FStatus); // 草稿
    }
}
