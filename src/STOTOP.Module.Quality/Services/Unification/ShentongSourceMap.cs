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
/// <param name="OnlyFilterColumn">「仅X」过滤列名（事件类用，无则 null）。与 <see cref="OnlyFilterEquals"/> 配对：
///   WHERE 该列 = <see cref="OnlyFilterEquals"/>。二者任一为 null 则不过滤、整源入事件。</param>
/// <param name="OnlyFilterEquals">「仅X」过滤值（与 <see cref="OnlyFilterColumn"/> 配对）。</param>
/// <param name="ProblemTypeConstant">问题类型常量（与 <see cref="ProblemTypeColumn"/> 二选一：列优先，列为 null 用常量）。</param>
/// <param name="AmountColumn">金额列名（→ 事件 F考核金额，TryDecimal；null 不填）。</param>
/// <param name="KeySnapshotColumns">写入 F关键字段JSON 的列集（null/空则取该源全部声明的关键业务列）。</param>
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
    string ProblemCodePrefix,
    string? OnlyFilterColumn = null,
    string? OnlyFilterEquals = null,
    string? ProblemTypeConstant = null,
    string? AmountColumn = null,
    string[]? KeySnapshotColumns = null);

/// <summary>
/// 申通各 STG 源 → 归一目标的静态映射表。扩展点：C1/C2/C3 增量加条目。
/// 本任务（C0）只含「物流完整性明细」一条（事件类，域=物流信息）。
/// </summary>
public static class ShentongSourceMap
{
    /// <summary>STG申通_物流完整性明细 表名常量。</summary>
    public const string LogisticsCompletenessTable = "STG申通_物流完整性明细";

    /// <summary>STG申通_物流及时准确明细 表名常量（C3 通用事件源 worked example）。</summary>
    public const string LogisticsTimelinessTable = "STG申通_物流及时准确明细";

    /// <summary>STG申通_小件员履约指标 表名常量（C1 员工日指标源）。</summary>
    public const string CourierFulfillTable = "STG申通_小件员履约指标";

    /// <summary>STG申通_积压监控汇总 表名常量（C2 网点日指标源，积压与遗失子集）。</summary>
    public const string BacklogMonitorTable = "STG申通_积压监控汇总";

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

            // C1：小件员履约指标 → 员工日质量指标（员工级，1 行/网点/小件员）。
            // 网点仅有名称（无编码列），员工仅有脏名（无工号列），源无日期列（业务日期取批次创建日，详见服务内 TODO）。
            [CourierFulfillTable] = new ShentongSourceDescriptor(
                StgTableName: CourierFulfillTable,
                QualityDomain: "员工综合",
                TargetKind: UnifyTargetKind.EmployeeMetric,
                NetworkCodeColumn: null,            // 本源无网点编码列，仅有名称
                NetworkNameColumn: "F所属网点",
                EmployeeNoColumn: null,             // 本源无工号列，仅有脏名
                EmployeeNameColumn: "F所属小件员",
                ProblemTypeColumn: null,            // 指标类无问题类型列
                DateColumn: null,                   // 本源无日期列，取批次日（TODO：申通补日期列后改读源列）
                WaybillColumn: null,
                PlatformColumn: null,
                ProblemCodePrefix: "CRFL"),

            // C2：积压监控汇总 → 网点日质量指标（网点级，1 行/网点/日期）。本源<b>有网点编码</b>。
            // 网点指标由多个 NetworkMetric 源各填子集、按 网点×日 合并 upsert；本源只填「积压与遗失」子集
            // （积压倍数/超N天积压量/遗失率ppm/遗失量/进港投诉/虚签投诉等），其它源字段（出仓/滞留/签收等）留 null 待 C3 填。
            [BacklogMonitorTable] = new ShentongSourceDescriptor(
                StgTableName: BacklogMonitorTable,
                QualityDomain: "积压与遗失",
                TargetKind: UnifyTargetKind.NetworkMetric,
                NetworkCodeColumn: "F网点编码",   // 本源有网点编码列
                NetworkNameColumn: "F网点名称",
                EmployeeNoColumn: null,            // 网点级，无员工维度
                EmployeeNameColumn: null,
                ProblemTypeColumn: null,           // 指标类无问题类型列
                DateColumn: "F统计日期",
                WaybillColumn: null,
                PlatformColumn: null,
                ProblemCodePrefix: "BKLG"),

            // C3 worked example：物流及时准确明细 → 质量事件（通用事件路径，第一个非 typed 事件源）。
            // 三个源文件（到件晚于签收/派件晚于签收/揽收上传不及时）同结构，靠「F问题类型」列区分事件类型。
            // 网点仅有名称（无编码列），员工取派件员（编号+名称），问题类型来自列 F问题类型，无金额列、无「仅X」过滤（整源入事件）。
            // 走通用路径 UnifyGenericEventAsync：列名全部来自本描述符，行用 StgRawReader（raw-SQL 按列名读）取。
            [LogisticsTimelinessTable] = new ShentongSourceDescriptor(
                StgTableName: LogisticsTimelinessTable,
                QualityDomain: "物流信息",
                TargetKind: UnifyTargetKind.Event,
                NetworkCodeColumn: null,            // 本源无网点编码列，仅有名称
                NetworkNameColumn: "F网点名称",
                EmployeeNoColumn: "F派件员编号",
                EmployeeNameColumn: "F派件员名称",
                ProblemTypeColumn: "F问题类型",
                DateColumn: "F统计日期",
                WaybillColumn: "F运单号",
                PlatformColumn: "F订单平台",
                ProblemCodePrefix: "LOGT",
                // 无「仅X」过滤（OnlyFilter* 留 null）、无问题类型常量（用列）、无金额列。
                // 关键字段快照列：日期/运单/网点/问题类型/扫描时间/派件员，便于审计回溯。
                KeySnapshotColumns: new[] { "F统计日期", "F运单号", "F网点名称", "F所属网点名称", "F问题类型", "F扫描时间", "F扫描员", "F扫描员编号", "F订单平台", "F派件员编号", "F派件员名称" }),
        };
}
