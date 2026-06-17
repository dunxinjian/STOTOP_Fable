using System;
using STOTOP.Module.Finance.Services;
using Xunit;

namespace STOTOP.Module.Finance.Tests.Amoeba;

/// <summary>
/// 批次5-S3 周期粒度纯函数单测（设计 spec §5.3）。
/// PeriodToDateRange 返回各粒度「闭区间末日」（聚合内部 &lt; end.AddDays(1) 自动转半开）；
/// CalcYoyPeriod 对 day/week 返回 null（无同比意义）。
/// </summary>
public class AmoebaPeriodGranularityTests
{
    [Theory]
    [InlineData("202603", "month", "2026-03-01", "2026-03-31")]
    [InlineData("20260315", "day", "2026-03-15", "2026-03-15")]
    [InlineData("2026-W11", "week", "2026-03-09", "2026-03-15")]   // ISO 2026 第11周：周一 3/9 → 周日 3/15
    [InlineData("2026Q2", "quarter", "2026-04-01", "2026-06-30")]
    [InlineData("2026", "year", "2026-01-01", "2026-12-31")]
    public void PeriodToDateRange_returns_inclusive_last_day_per_granularity(
        string period, string granularity, string start, string end)
    {
        var (s, e) = AmoebaPLService.PeriodToDateRange(period, granularity);
        Assert.Equal(DateTime.Parse(start), s);
        Assert.Equal(DateTime.Parse(end), e);
    }

    [Fact]
    public void PeriodToDateRange_defaults_to_month()
    {
        var (s, e) = AmoebaPLService.PeriodToDateRange("202602");
        Assert.Equal(new DateTime(2026, 2, 1), s);
        Assert.Equal(new DateTime(2026, 2, 28), e);
    }

    [Theory]
    [InlineData("202603", "month", true)]
    [InlineData("202613", "month", false)]   // 月份越界
    [InlineData("2026", "month", false)]      // 位数不符
    [InlineData("20260315", "day", true)]
    [InlineData("20260230", "day", false)]    // 2月30日非法
    [InlineData("2026-W11", "week", true)]
    [InlineData("2026-W54", "week", false)]   // 周越界
    [InlineData("2026Q1", "quarter", true)]
    [InlineData("2026Q5", "quarter", false)]  // 季越界
    [InlineData("2026", "year", true)]
    [InlineData("abcd", "year", false)]
    public void IsValidPeriod_validates_format_per_granularity(string period, string granularity, bool expected)
    {
        Assert.Equal(expected, AmoebaPLService.IsValidPeriod(period, granularity));
    }

    [Theory]
    [InlineData("202601", "month", "202512")]
    [InlineData("202603", "month", "202602")]
    [InlineData("20260301", "day", "20260228")]
    [InlineData("2026-W02", "week", "2026-W01")]
    [InlineData("2026Q1", "quarter", "2025Q4")]
    [InlineData("2026Q3", "quarter", "2026Q2")]
    [InlineData("2026", "year", "2025")]
    public void CalcPreviousPeriod_steps_back_one_unit(string period, string granularity, string expected)
    {
        Assert.Equal(expected, AmoebaPLService.CalcPreviousPeriod(period, granularity));
    }

    [Theory]
    [InlineData("202603", "month", "202503")]
    [InlineData("2026Q2", "quarter", "2025Q2")]
    [InlineData("2026", "year", "2025")]
    public void CalcYoyPeriod_subtracts_one_year_for_ledger_granularities(string period, string granularity, string expected)
    {
        Assert.Equal(expected, AmoebaPLService.CalcYoyPeriod(period, granularity));
    }

    [Theory]
    [InlineData("20260315", "day")]
    [InlineData("2026-W11", "week")]
    public void CalcYoyPeriod_returns_null_for_sub_month_granularities(string period, string granularity)
    {
        Assert.Null(AmoebaPLService.CalcYoyPeriod(period, granularity));
    }

    [Theory]
    [InlineData("202603", "month", "M:202603")]
    [InlineData("20260315", "day", "D:20260315")]
    [InlineData("2026-W11", "week", "W:2026-W11")]
    [InlineData("2026Q1", "quarter", "Q:2026Q1")]
    [InlineData("2026", "year", "Y:2026")]
    public void BuildPeriodKey_prefixes_by_granularity(string period, string granularity, string expected)
    {
        Assert.Equal(expected, AmoebaPLService.BuildPeriodKey(period, granularity));
    }
}
