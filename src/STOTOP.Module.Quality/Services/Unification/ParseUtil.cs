using System.Globalization;

namespace STOTOP.Module.Quality.Services.Unification;

/// <summary>
/// 归一阶段的轻量解析助手：把 STG 表里以字符串落地的业务列（日期/小数/整数/布尔）安全转成强类型。
/// 全部容错（解析失败返回 null/false），不抛异常——脏数据是常态，归一不应因单格解析失败而中断。
/// </summary>
public static class ParseUtil
{
    /// <summary>
    /// 尝试解析日期；失败返回 null。
    /// 先走通用 <see cref="DateTime.TryParse(string?, out DateTime)"/>（覆盖 2026-06-15 / 2026/6/15 等带分隔格式），
    /// 再退化按紧凑 yyyyMMdd（如申通积压监控的「20260615」，通用 TryParse 解不出）精确解析。
    /// </summary>
    public static DateTime? TryDate(string? s)
    {
        if (DateTime.TryParse(s, out var d)) return d;
        if (DateTime.TryParseExact((s ?? "").Trim(), "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2)) return d2;
        return null;
    }

    /// <summary>尝试解析小数（去尾部空白与百分号）；失败返回 null。注意：百分号场景仅去符号、不除以 100。</summary>
    public static decimal? TryDecimal(string? s) =>
        decimal.TryParse((s ?? "").Trim().TrimEnd('%'), out var v) ? v : (decimal?)null;

    /// <summary>尝试解析整数；整数失败时退化为小数后截断；再失败返回 null。</summary>
    public static int? TryInt(string? s) =>
        int.TryParse(s, out var v) ? v : (decimal.TryParse(s, out var dv) ? (int)dv : (int?)null);

    /// <summary>真值判定：是/1/true/True 视为真，其余为假。</summary>
    public static bool Truthy(string? s) => s is "是" or "1" or "true" or "True";

    /// <summary>把日期格式化为 yyyyMM 统计月；null 入参返回 null。</summary>
    public static string? Ym(DateTime? d) => d?.ToString("yyyyMM", CultureInfo.InvariantCulture);
}
