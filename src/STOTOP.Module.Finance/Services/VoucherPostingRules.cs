using STOTOP.Module.Finance.Entities;

namespace STOTOP.Module.Finance.Services;

/// <summary>
/// 凭证落库纯规则：期间解析、结账校验、归属鉴权。
/// 不依赖 DbContext，便于单元测试；VoucherService 负责取数后委派到这里。
/// </summary>
public static class VoucherPostingRules
{
    /// <summary>
    /// 按 日期 + 账套 在候选期间中定位唯一期间；查不到抛错（缺账期一律拒绝落库）。
    /// </summary>
    public static FinAccountPeriod ResolvePeriod(
        IEnumerable<FinAccountPeriod> candidates, DateTime date, long accountSetId)
    {
        var period = candidates
            .Where(p => p.FAccountSetId == accountSetId
                && p.FStartDate <= date
                && p.FEndDate >= date)
            .OrderByDescending(p => p.FStartDate)
            .FirstOrDefault();
        if (period == null)
            throw new InvalidOperationException(
                $"未找到账套 {accountSetId} 在 {date:yyyy-MM-dd} 对应的账期，请先建立该期间");
        return period;
    }
}
