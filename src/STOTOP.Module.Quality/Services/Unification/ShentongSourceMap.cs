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

    // ── C3 批次1：5 个事件源（全走通用事件路径 UnifyGenericEventAsync，仅加描述符）──
    /// <summary>STG申通_揽收分析明细 表名常量（C3 批次1，揽收时效，仅「未及时」过滤入事件）。</summary>
    public const string PickupAnalysisTable = "STG申通_揽收分析明细";

    /// <summary>STG申通_未出仓监控明细 表名常量（C3 批次1，出仓时效，整源即未出仓）。</summary>
    public const string OutboundMonitorTable = "STG申通_未出仓监控明细";

    /// <summary>STG申通_交货滞留明细 表名常量（C3 批次1，交货滞留，仅「滞留」过滤入事件）。</summary>
    public const string HandoverDelayTable = "STG申通_交货滞留明细";

    /// <summary>STG申通_末端派送考核明细 表名常量（C3 批次1，派送签收时效，整源入）。</summary>
    public const string DeliveryAssessTable = "STG申通_末端派送考核明细";

    /// <summary>STG申通_签收未达标明细 表名常量（C3 批次1，派送签收时效，整源即未达标）。</summary>
    public const string SignSubstandardTable = "STG申通_签收未达标明细";

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

            // ─────────────────────────────────────────────────────────────────────
            // C3 批次1：5 个事件源（全走通用事件路径，仅加描述符；列名已逐源核对实体确认）。
            // ─────────────────────────────────────────────────────────────────────

            // ① 揽收分析明细 → 质量事件（域=揽收时效）。网点仅有名称（F揽收所属网点），员工=F收件员（仅脏名，无工号列）。
            //    问题类型用常量「揽收不及时」（源无问题类型列）。运单列是 F运单编号（非 F运单号）；本源无电商平台映射列（有 F电商平台但与其它源平台口径一致，纳入快照不作平台维度）。
            //    「仅未及时」过滤：F揽收及时标识 = 否（抽样确认：标识取值为「是/否」，未及时=「否」；本样例 455 行全为「否」即全部未及时，过滤选中全部）。
            [PickupAnalysisTable] = new ShentongSourceDescriptor(
                StgTableName: PickupAnalysisTable,
                QualityDomain: "揽收时效",
                TargetKind: UnifyTargetKind.Event,
                NetworkCodeColumn: null,             // 本源无网点编码列，仅有名称
                NetworkNameColumn: "F揽收所属网点",
                EmployeeNoColumn: null,              // 本源无工号列，仅 F收件员 脏名
                EmployeeNameColumn: "F收件员",
                ProblemTypeColumn: null,             // 无问题类型列 → 用常量
                DateColumn: "F统计日期",
                WaybillColumn: "F运单编号",          // 本源运单列是「运单编号」
                PlatformColumn: "F电商平台",
                ProblemCodePrefix: "PKUP",
                OnlyFilterColumn: "F揽收及时标识",   // 仅未及时入事件
                OnlyFilterEquals: "否",              // 抽样确认：未及时=「否」（是/否标识）
                ProblemTypeConstant: "揽收不及时",
                KeySnapshotColumns: new[] { "F统计日期", "F运单编号", "F揽收所属网点", "F收件员", "F揽收及时标识", "F揽收时间", "F揽收截止时间", "F订单揽收用时h", "F电商平台" }),

            // ② 未出仓监控明细 → 质量事件（域=出仓时效）。整源即「未出仓」，无过滤、问题类型用常量「未及时出仓」。
            //    网点有名称 F应签所属网点（也有编码列 F应签所属网点编码，但与其它源统一按名称匹配，编码纳入快照）；员工=F派件员（仅脏名）。
            //    本源无电商平台列。
            [OutboundMonitorTable] = new ShentongSourceDescriptor(
                StgTableName: OutboundMonitorTable,
                QualityDomain: "出仓时效",
                TargetKind: UnifyTargetKind.Event,
                NetworkCodeColumn: null,             // 统一按名称匹配；编码列入快照
                NetworkNameColumn: "F应签所属网点",
                EmployeeNoColumn: null,
                EmployeeNameColumn: "F派件员",
                ProblemTypeColumn: null,             // 无问题类型列 → 用常量
                DateColumn: "F统计日期",
                WaybillColumn: "F运单号",
                PlatformColumn: null,                // 本源无电商平台列
                ProblemCodePrefix: "OUTB",
                // 整源入：实体无明确「是否未出仓」单列，样例亦无正常行混入（导出本身=未出仓实时监控）→ 不过滤。
                ProblemTypeConstant: "未及时出仓",
                KeySnapshotColumns: new[] { "F统计日期", "F运单号", "F应签所属网点", "F应签所属网点编码", "F派件员", "F实际出仓时间", "F理论应出仓时间", "F理论应出仓日期", "F出仓距离", "F三段码" }),

            // ③ 交货滞留明细 → 质量事件（域=交货滞留）。网点仅有名称 F揽收所属网点；员工=F揽收小件员名称（脏名，带「揽收」前缀，匹配器启发式处理）。
            //    问题类型常量「交货滞留」（源无问题类型列）。
            //    「仅滞留」过滤：F交货滞留标识 = 是（抽样确认：是/否标识，滞留=「是」；本样例 380 行全为「是」即全部滞留，过滤选中全部）。
            [HandoverDelayTable] = new ShentongSourceDescriptor(
                StgTableName: HandoverDelayTable,
                QualityDomain: "交货滞留",
                TargetKind: UnifyTargetKind.Event,
                NetworkCodeColumn: null,             // 本源无网点编码列，仅有名称
                NetworkNameColumn: "F揽收所属网点",
                EmployeeNoColumn: null,              // 仅脏名，无工号列
                EmployeeNameColumn: "F揽收小件员名称",
                ProblemTypeColumn: null,             // 无问题类型列 → 用常量
                DateColumn: "F业务日期",             // 本源日期列是「业务日期」
                WaybillColumn: "F运单号",
                PlatformColumn: "F电商平台",
                ProblemCodePrefix: "HDOV",
                OnlyFilterColumn: "F交货滞留标识",   // 仅滞留入事件
                OnlyFilterEquals: "是",              // 抽样确认：滞留=「是」（是/否标识）
                ProblemTypeConstant: "交货滞留",
                KeySnapshotColumns: new[] { "F业务日期", "F运单号", "F揽收所属网点", "F揽收小件员名称", "F交货滞留标识", "F交货时间", "F交货截止时间", "F网点装车时间", "F当前交货状态", "F电商平台" }),

            // ④ 末端派送考核明细 → 质量事件（域=派送签收时效）。网点仅有名称 F应签收所属网点名称；员工=F派件员姓名（脏名）。
            //    问题类型用列 F问题件类型名称（无值=空 → 落「(未分类)」；源无该列时回退常量「签收延迟」，本源有该列故用列）。
            //    【一期口径抉择 — 整源入，不过滤】依据：该源延迟判定分散在多个独立标识列（T0/T1/T2/T3 延迟、当天签收延迟0-24h/24-48h/超48h、一/二阶段内签收 等），
            //      单列等值过滤覆盖不全。抽样：F未签收有问题件标识=是 仅 46/345 行（恰=F问题件类型名称 非空的 46 行），
            //      若以它过滤会漏掉其余 299 行（这些行 F已签收标识=否、F当天签收标识=否，仍属签收时效问题）。
            //      该导出本身=末端派送考核（未签收/延迟全集，F已签收标识 全为「否」），无清晰单列「问题/正常」判定 → 一期不过滤、整源入，
            //      用 F问题件类型名称 当问题类型（46 行有具体类型，299 行无类型落「(未分类)」），宁可全量入、勿漏，噪声二期检测规则再收。
            [DeliveryAssessTable] = new ShentongSourceDescriptor(
                StgTableName: DeliveryAssessTable,
                QualityDomain: "派送签收时效",
                TargetKind: UnifyTargetKind.Event,
                NetworkCodeColumn: null,             // 本源无网点编码列，仅有名称
                NetworkNameColumn: "F应签收所属网点名称",
                EmployeeNoColumn: null,              // 仅脏名，无工号列
                EmployeeNameColumn: "F派件员姓名",
                ProblemTypeColumn: "F问题件类型名称", // 列优先（有该列）；空单元格落「(未分类)」
                DateColumn: "F统计日期",
                WaybillColumn: "F运单号",
                PlatformColumn: "F电商平台",
                ProblemCodePrefix: "DLVA",
                // 一期不过滤（OnlyFilter* 留 null，整源入）；常量仅在列为 null 时兜底（本源列存在，不触发）。
                ProblemTypeConstant: "签收延迟",
                KeySnapshotColumns: new[] { "F统计日期", "F运单号", "F应签收所属网点名称", "F派件员姓名", "F问题件类型名称", "F问题件原因", "F已签收标识", "F未签收有问题件标识", "F当天签收标识", "F派件时间", "F签收时间", "F电商平台" }),

            // ⑤ 签收未达标明细 → 质量事件（域=派送签收时效）。网点仅有名称 F应签网点；员工=F业务员（脏名）。
            //    问题类型常量「签收未达标」（源无问题类型列）。日期列 F应签日期（带「-」分隔，ParseUtil.TryDate 兼容）。
            //    整源入：实体无「未达标」单一标识列，导出本身=签收未达标全集（抽样 F当日签收标识/F是否已签收 全为「否」，无正常行混入）→ 不过滤。
            [SignSubstandardTable] = new ShentongSourceDescriptor(
                StgTableName: SignSubstandardTable,
                QualityDomain: "派送签收时效",
                TargetKind: UnifyTargetKind.Event,
                NetworkCodeColumn: null,             // 本源无网点编码列，仅有名称
                NetworkNameColumn: "F应签网点",
                EmployeeNoColumn: null,              // 仅脏名，无工号列
                EmployeeNameColumn: "F业务员",
                ProblemTypeColumn: null,             // 无问题类型列 → 用常量
                DateColumn: "F应签日期",
                WaybillColumn: "F运单号",
                PlatformColumn: null,                // 本源无电商平台列
                ProblemCodePrefix: "SIGN",
                // 整源入（OnlyFilter* 留 null）；抽样确认 F当日签收标识=否、F是否已签收=否 全量，无正常行 → 不过滤。
                ProblemTypeConstant: "签收未达标",
                KeySnapshotColumns: new[] { "F应签日期", "F运单号", "F应签网点", "F业务员", "F当日签收标识", "F是否已签收", "F是否未签收有问题件", "F签收时间", "F派件网点", "F签收网点" }),
        };
}
