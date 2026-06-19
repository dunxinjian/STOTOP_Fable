using STOTOP.Module.Finance.Entities;
using STOTOP.Module.Finance.Services;
using Xunit;

namespace STOTOP.Module.Finance.Tests.Voucher;

public class VoucherPostingRulesTests
{
    private static List<FinAccountPeriod> Periods() => new()
    {
        new FinAccountPeriod { FID = 10, FAccountSetId = 1, FYear = 2026, FPeriodNo = 5,
            FStartDate = new DateTime(2026, 5, 1), FEndDate = new DateTime(2026, 5, 31), FIsClosed = 1 },
        new FinAccountPeriod { FID = 11, FAccountSetId = 1, FYear = 2026, FPeriodNo = 6,
            FStartDate = new DateTime(2026, 6, 1), FEndDate = new DateTime(2026, 6, 30), FIsClosed = 0 },
        new FinAccountPeriod { FID = 99, FAccountSetId = 2, FYear = 2026, FPeriodNo = 6,
            FStartDate = new DateTime(2026, 6, 1), FEndDate = new DateTime(2026, 6, 30), FIsClosed = 0 },
    };

    [Fact]
    public void ResolvePeriod_returns_matching_period_for_date_and_account_set()
    {
        var p = VoucherPostingRules.ResolvePeriod(Periods(), new DateTime(2026, 6, 15), accountSetId: 1);
        Assert.Equal(11, p.FID);
    }

    [Fact]
    public void ResolvePeriod_ignores_other_account_sets()
    {
        var p = VoucherPostingRules.ResolvePeriod(Periods(), new DateTime(2026, 6, 15), accountSetId: 1);
        Assert.Equal(1, p.FAccountSetId);
    }

    [Fact]
    public void ResolvePeriod_throws_when_no_period_covers_date()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => VoucherPostingRules.ResolvePeriod(Periods(), new DateTime(2026, 7, 1), accountSetId: 1));
        Assert.Contains("未找到", ex.Message);
    }
}
