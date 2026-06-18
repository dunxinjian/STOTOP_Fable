namespace STOTOP.Module.Quality.Services.Unification;

/// <summary>
/// 归一目标种类：事件类（一行一质量事件，写 QL申通_承运商质量事件）/
/// 网点指标类（写 QL申通_网点日质量指标）/ 员工指标类（写 QL申通_员工日质量指标）。
/// 本任务（C0）仅落地事件类；指标类留给 C1/C2/C3。
/// </summary>
public enum UnifyTargetKind
{
    /// <summary>事件类：源每行 → 一条质量事件。</summary>
    Event,
    /// <summary>网点指标类：源每行 → 一条网点日指标 upsert。</summary>
    NetworkMetric,
    /// <summary>员工指标类：源每行 → 一条员工日指标 upsert。</summary>
    EmployeeMetric,
}

/// <summary>
/// 描述「某 STG 源表 → 质量域 / 目标种类 / 关键列」的映射。
/// 关键列名都是 STG 表里的列名（用于反射取值与主数据匹配），允许为空（该源无此维度）。
/// 后续 C1/C2/C3 增量加源时，只需向 <see cref="ShentongSourceMap.All"/> 追加一条 <see cref="ShentongSourceDescriptor"/>，
/// 归一服务按 <see cref="UnifyTargetKind"/> 分发处理，无需改服务主干。
/// </summary>
/// <param name="StgTableName">来源 STG 表名（也是事件 F来源STG表 的取值）。</param>
/// <param name="QualityDomain">质量域（写入字典/事件的 F质量域）。</param>
/// <param name="TargetKind">归一目标种类。</param>
/// <param name="NetworkCodeColumn">网点编码列名（无则 null）。</param>
/// <param name="NetworkNameColumn">网点名称列名（无则 null）。</param>
/// <param name="EmployeeNoColumn">员工工号列名（无则 null）。</param>
/// <param name="EmployeeNameColumn">员工姓名列名（无则 null）。</param>
/// <param name="ProblemTypeColumn">问题类型来源列名（事件类必填；无则 null）。</param>
/// <param name="DateColumn">业务日期列名（无则 null）。</param>
/// <param name="WaybillColumn">运单号列名（无则 null）。</param>
/// <param name="PlatformColumn">电商平台列名（无则 null）。</param>
/// <param name="ProblemCodePrefix">字典自建时问题类型编码前缀（如 "LOGI"）。</param>
public record ShentongSourceDescriptor(
    string StgTableName,
    string QualityDomain,
    UnifyTargetKind TargetKind,
    string? NetworkCodeColumn,
    string? NetworkNameColumn,
    string? EmployeeNoColumn,
    string? EmployeeNameColumn,
    string? ProblemTypeColumn,
    string? DateColumn,
    string? WaybillColumn,
    string? PlatformColumn,
    string ProblemCodePrefix);

/// <summary>
/// 申通各 STG 源 → 归一目标的静态映射表。扩展点：C1/C2/C3 增量加条目。
/// 本任务（C0）只含「物流完整性明细」一条（事件类，域=物流信息）。
/// </summary>
public static class ShentongSourceMap
{
    /// <summary>STG申通_物流完整性明细 表名常量。</summary>
    public const string LogisticsCompletenessTable = "STG申通_物流完整性明细";

    /// <summary>
    /// 全部源描述（按 STG 表名索引）。后续加源 = 这里追加一条。
    /// </summary>
    public static readonly IReadOnlyDictionary<string, ShentongSourceDescriptor> All =
        new Dictionary<string, ShentongSourceDescriptor>
        {
            [LogisticsCompletenessTable] = new ShentongSourceDescriptor(
                StgTableName: LogisticsCompletenessTable,
                QualityDomain: "物流信息",
                TargetKind: UnifyTargetKind.Event,
                NetworkCodeColumn: null,            // 本源无网点编码列，仅有名称
                NetworkNameColumn: "F网点名称",
                EmployeeNoColumn: "F签收员编号",
                EmployeeNameColumn: "F签收员名称",
                ProblemTypeColumn: "F问题类型",
                DateColumn: "F统计日期",
                WaybillColumn: "F运单号",
                PlatformColumn: "F订单平台",
                ProblemCodePrefix: "LOGI"),
        };
}
