using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.Quality.Entities;

namespace STOTOP.Module.Quality.Services.Unification;

/// <summary>
/// 归一服务实现。C0 落地「事件类（物流完整性明细）→ 质量事件」的 worked example，
/// 确立后续所有事件类源的统一写入模式：
/// 主数据匹配（网点按名称、员工按工号/别名/启发式）→ 问题字典查/自建 → 按唯一键 upsert 质量事件。
///
/// 关键约束：
/// - 员工启发式候选（Status==3）<b>不绑工号/ID</b>，只保留姓名原文（评审强调，避免错绑）。
/// - 幂等：质量事件按唯一键 (FOrgId,F承运商,F来源STG表,F来源行ID) upsert；问题字典按唯一键
///   (FOrgId,F承运商,F质量域,F来源问题类型原文) 查/建。重复跑不增行、不重复建字典。
/// </summary>
public class QualityUnificationService : IQualityUnificationService
{
    private readonly STOTOPDbContext _db;
    private readonly IMasterDataMatcher _matcher;

    public QualityUnificationService(STOTOPDbContext db, IMasterDataMatcher matcher)
    {
        _db = db;
        _matcher = matcher;
    }

    public async global::System.Threading.Tasks.Task<UnifyResult> UnifyShentongAsync(long orgId, CancellationToken ct = default)
    {
        // 泛化分发：遍历源映射，按 TargetKind 分派到对应路径，累计五项计数。
        // 加源 = 向 ShentongSourceMap.All 追加一条 + （新种类时）在此补一个 switch 分支。
        int events = 0, empUpserts = 0, netUpserts = 0, netUnmatched = 0, empUnmatched = 0;

        foreach (var desc in ShentongSourceMap.All.Values)
        {
            var r = desc.TargetKind switch
            {
                UnifyTargetKind.Event => await UnifyLogisticsCompletenessAsync(orgId, desc, ct),
                UnifyTargetKind.EmployeeMetric => await UnifyCourierFulfillAsync(orgId, desc, ct),
                // NetworkMetric：C2 落地积压监控子集。多个 NetworkMetric 源各填子集、按 网点×日 合并 upsert。
                // 目前 ShentongSourceMap 里仅积压监控一条，故直接路由；C3 增源时按源表名细分（或在描述符上加路由键）。
                UnifyTargetKind.NetworkMetric => await UnifyBacklogMonitorAsync(orgId, desc, ct),
                _ => throw new NotSupportedException($"未支持的归一目标种类 {desc.TargetKind}（源 {desc.StgTableName}）"),
            };
            events += r.EventsUpserted;
            empUpserts += r.EmployeeMetricUpserts;
            netUpserts += r.NetworkMetricUpserts;
            netUnmatched += r.NetworkUnmatched;
            empUnmatched += r.EmployeeUnmatched;
        }

        return new UnifyResult(events, empUpserts, netUpserts, netUnmatched, empUnmatched);
    }

    /// <summary>
    /// 事件类 worked example：STG申通_物流完整性明细 → QL申通_承运商质量事件。
    /// </summary>
    private async global::System.Threading.Tasks.Task<UnifyResult> UnifyLogisticsCompletenessAsync(
        long orgId, ShentongSourceDescriptor desc, CancellationToken ct)
    {
        // 只投影需要的列（避免读全实体时碰到 STG 表里允许 NULL、但实体声明为非空 long 的系统列，
        // 如 F账套ID NULL → SqlNullValueException）。投影后即纯内存对象，匹配/写入都基于它。
        var rows = await _db.Set<StgShentongLogisticsCompleteness>()
            .IgnoreQueryFilters()
            .Where(r => r.FOrgId == orgId && !r.FIsRevoked)
            .Select(r => new LogisticsRow(
                r.FID, r.F批次ID, r.F统计日期, r.F运单号, r.F网点名称, r.F所属网点名称,
                r.F问题类型, r.F订单平台, r.F签收员编号, r.F签收员名称))
            .ToListAsync(ct);

        int events = 0, networkUnmatched = 0, employeeUnmatched = 0;

        // 本次运行内的字典缓存：同一原文只查/建一次（避免逐行重复查库，且同批多行同问题不会重复建）
        var dictCache = new Dictionary<string, QlShentongProblemDict>();

        foreach (var row in rows)
        {
            // ── 主数据匹配：网点（本源无编码，传名称）→ 员工（带网点编码上下文）──
            var net = await _matcher.ResolveNetworkAsync(null, row.F网点名称, orgId, ct);
            var emp = await _matcher.ResolveEmployeeAsync(row.F签收员编号, row.F签收员名称, net.Code, orgId, ct);

            // ── 问题字典查/建（按唯一键，从数据自动长出，人工后续完善）──
            var problemRaw = (row.F问题类型 ?? "").Trim();
            var dict = await GetOrCreateProblemDictAsync(orgId, desc, problemRaw, dictCache, ct);

            // ── upsert 质量事件（唯一键：来源STG表 + 来源行ID）──
            var existing = await _db.Set<QlShentongQualityEvent>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e =>
                    e.FOrgId == orgId &&
                    e.F承运商 == "申通" &&
                    e.F来源STG表 == desc.StgTableName &&
                    e.F来源行ID == row.FID, ct);

            var ev = existing ?? new QlShentongQualityEvent();

            ev.FOrgId = orgId;
            ev.F承运商 = "申通";
            ev.F业务日期 = ParseUtil.TryDate(row.F统计日期);
            ev.F统计年月 = ParseUtil.Ym(ev.F业务日期);
            ev.F运单号 = row.F运单号;

            // 网点：编码取匹配结果，名称保留原文，状态回填
            ev.F网点编码 = net.Code;
            ev.F网点名称 = row.F网点名称;
            ev.F网点匹配状态 = net.Status;

            // 员工：Status 1/2 绑工号+ID；Status 3（启发式候选）不绑，仅保留姓名原文
            if (emp.Status is 1 or 2)
            {
                ev.F员工工号 = emp.EmployeeNo;
                ev.F员工ID = emp.EmployeeId;
            }
            else
            {
                ev.F员工工号 = null;
                ev.F员工ID = null;
            }
            ev.F员工姓名原文 = row.F签收员名称;
            ev.F员工匹配状态 = emp.Status;

            ev.F电商平台 = row.F订单平台;
            ev.F质量域 = desc.QualityDomain;

            // 问题类型 / 严重度 / 是否考核 取字典
            ev.F问题类型编码 = dict.F问题类型编码;
            ev.F问题类型名称 = dict.F问题类型名称;
            ev.F严重度 = dict.F默认严重度;
            ev.F是否考核件 = dict.F是否考核;

            ev.F来源STG表 = desc.StgTableName;
            ev.F来源行ID = row.FID;
            ev.F来源批次ID = row.F批次ID;
            ev.F关键字段JSON = BuildKeySnapshot(row);

            if (existing == null)
            {
                ev.F创建时间 = DateTime.Now;
                _db.Set<QlShentongQualityEvent>().Add(ev);
            }

            events++;
            if (net.Status == 0) networkUnmatched++;
            if (emp.Status is 0 or 3) employeeUnmatched++;
        }

        await _db.SaveChangesAsync(ct);

        return new UnifyResult(
            EventsUpserted: events,
            EmployeeMetricUpserts: 0,
            NetworkMetricUpserts: 0,
            NetworkUnmatched: networkUnmatched,
            EmployeeUnmatched: employeeUnmatched);
    }

    /// <summary>
    /// 员工指标类路径：STG申通_小件员履约指标 → QL申通_员工日质量指标。
    /// 员工级粒度（1 行/网点/小件员）。本源<b>无日期列</b>，业务日期取批次创建日；
    /// 员工<b>无工号、仅脏名</b>，仅当主数据匹配到工号（emp.Status∈{1,2}）才建指标（工号是唯一键一部分）。
    /// 唯一键 (FOrgId,F承运商,F业务日期,F网点编码,F员工工号)，按键 upsert，重复跑不增行（幂等）。
    /// </summary>
    private async global::System.Threading.Tasks.Task<UnifyResult> UnifyCourierFulfillAsync(
        long orgId, ShentongSourceDescriptor desc, CancellationToken ct)
    {
        // 只投影需要的列（避免读全实体碰 STG 表允许 NULL、实体声明非空 long 的系统列，如 F账套ID NULL → SqlNullValueException）。
        var rows = await _db.Set<StgShentongCourierFulfill>()
            .IgnoreQueryFilters()
            .Where(r => r.FOrgId == orgId && !r.FIsRevoked)
            .Select(r => new CourierRow(
                r.FID, r.F批次ID, r.F所属网点, r.F所属小件员,
                r.F当日派签量, r.F当日派签率, r.F应上门量, r.F未上门量, r.F按需上门率,
                r.F客诉发起量, r.F工单定责量, r.F客诉发起率,
                r.F虚假电联, r.F无效电联, r.F双签, r.F照片定位虚假, r.F签收文本不规范, r.F引导代收,
                r.F回访真实率))
            .ToListAsync(ct);

        // 一次性查本批次集合的 CfBatch.F创建时间，建 F批次ID → 批次日(.Date) 映射。
        var batchIds = rows.Select(r => r.F批次ID).Distinct().ToList();
        var batchDays = await _db.Set<CfBatch>()
            .IgnoreQueryFilters()
            .Where(b => batchIds.Contains(b.FID))
            .Select(b => new { b.FID, b.FCreatedTime })
            .ToDictionaryAsync(x => x.FID, x => x.FCreatedTime.Date, ct);

        int empUpserts = 0, networkUnmatched = 0, employeeUnmatched = 0;

        // 同批去重缓存：键 = (业务日期, 网点编码, 工号)。
        // 防止同一次归一里出现两条相同唯一键行时，第二行查库查不到第一行刚 Add 但未 SaveChanges 的待插实体，
        // 又 Add 第二个 → SaveChangesAsync 撞 DB 唯一索引 → 整批回滚。命中缓存则复用同一实例继续 upsert。
        var metricCache = new Dictionary<(DateTime, string, string), QlShentongEmployeeDailyMetric>();

        foreach (var row in rows)
        {
            // ── 主数据匹配：网点（本源无编码，传名称）→ 员工（脏名，无工号）──
            var net = await _matcher.ResolveNetworkAsync(null, row.F所属网点, orgId, ct);
            var emp = await _matcher.ResolveEmployeeAsync(null, row.F所属小件员, net.Code, orgId, ct);

            if (net.Status == 0) networkUnmatched++;

            // 业务日期 = 批次创建日。TODO：用户将让申通给该导出加日期列；届时改读源日期列（desc.DateColumn）。
            var bizDate = batchDays.TryGetValue(row.F批次ID, out var d) ? d : DateTime.Today;

            // 仅 emp.Status∈{1,2}（匹配到工号）才 upsert 员工日指标——工号是唯一键一部分，未确定工号无法建键。
            // 未匹配(Status 0/3)→ 不建指标，计入 EmployeeUnmatched；待人工认领+补别名后重跑生成。
            if (emp.Status is not (1 or 2))
            {
                employeeUnmatched++;
                continue;
            }

            // ── upsert 员工日指标（唯一键：工号 × 业务日期 × 网点编码，含 FOrgId/F承运商）──
            // 先查同批缓存（命中=本批前面已处理过同键行，复用实例，isNew=false 避免重复 Add）；
            // 未命中再查库；库无则 new+Add+入缓存。
            var cacheKey = (bizDate, net.Code!, emp.EmployeeNo!);
            bool isNew = false;
            if (!metricCache.TryGetValue(cacheKey, out var metric))
            {
                var existing = await _db.Set<QlShentongEmployeeDailyMetric>()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(m =>
                        m.FOrgId == orgId &&
                        m.F承运商 == "申通" &&
                        m.F业务日期 == bizDate &&
                        m.F网点编码 == net.Code &&
                        m.F员工工号 == emp.EmployeeNo, ct);

                isNew = existing == null;
                metric = existing ?? new QlShentongEmployeeDailyMetric();
                metricCache[cacheKey] = metric;
            }

            metric.FOrgId = orgId;
            metric.F承运商 = "申通";
            metric.F业务日期 = bizDate;
            metric.F统计年月 = ParseUtil.Ym(bizDate);
            metric.F网点编码 = net.Code!;
            metric.F员工工号 = emp.EmployeeNo!;
            metric.F员工姓名原文 = row.F所属小件员;   // 脏名原文
            metric.F员工ID = emp.EmployeeId;

            // ── 派签 ──
            metric.F当日派签量 = ParseUtil.TryInt(row.F当日派签量);
            metric.F当日派签率 = ParseUtil.TryDecimal(row.F当日派签率);
            metric.F应上门量 = ParseUtil.TryInt(row.F应上门量);
            metric.F未上门量 = ParseUtil.TryInt(row.F未上门量);
            metric.F按需上门率 = ParseUtil.TryDecimal(row.F按需上门率);

            // ── 客诉 ──
            metric.F客诉发起量 = ParseUtil.TryInt(row.F客诉发起量);
            metric.F工单定责量 = ParseUtil.TryInt(row.F工单定责量);
            metric.F客诉发起率 = ParseUtil.TryDecimal(row.F客诉发起率);

            // ── 违规 ──
            metric.F违规虚假电联 = ParseUtil.TryInt(row.F虚假电联);
            metric.F违规无效电联 = ParseUtil.TryInt(row.F无效电联);
            metric.F违规双签 = ParseUtil.TryInt(row.F双签);
            metric.F违规照片定位虚假 = ParseUtil.TryInt(row.F照片定位虚假);
            metric.F违规签收文本不规范 = ParseUtil.TryInt(row.F签收文本不规范);
            metric.F违规引导代收 = ParseUtil.TryInt(row.F引导代收);
            metric.F回访真实率 = ParseUtil.TryDecimal(row.F回访真实率);

            // 事件类才有的字段（F虚假签收数/F照片质检不合格数/F派送超时T0-T3/F问题件数/F考核金额合计 等）
            // 来自事件聚合，非本源直供，本期留 null（不覆盖既有值——upsert 时仅本源字段被刷新）。

            metric.F来源批次ID = row.F批次ID;

            if (isNew)
            {
                metric.F创建时间 = DateTime.Now;
                _db.Set<QlShentongEmployeeDailyMetric>().Add(metric);
            }

            empUpserts++;
        }

        await _db.SaveChangesAsync(ct);

        return new UnifyResult(
            EventsUpserted: 0,
            EmployeeMetricUpserts: empUpserts,
            NetworkMetricUpserts: 0,
            NetworkUnmatched: networkUnmatched,
            EmployeeUnmatched: employeeUnmatched);
    }

    /// <summary>
    /// 小件员履约源行的内存投影（只含归一需要的列），避免读全实体碰 NULL 系统列。
    /// </summary>
    private sealed record CourierRow(
        long FID,
        long F批次ID,
        string? F所属网点,
        string? F所属小件员,
        string? F当日派签量,
        string? F当日派签率,
        string? F应上门量,
        string? F未上门量,
        string? F按需上门率,
        string? F客诉发起量,
        string? F工单定责量,
        string? F客诉发起率,
        string? F虚假电联,
        string? F无效电联,
        string? F双签,
        string? F照片定位虚假,
        string? F签收文本不规范,
        string? F引导代收,
        string? F回访真实率);

    /// <summary>
    /// 网点指标类路径（<b>多源合并 upsert 模板</b>）：STG申通_积压监控汇总 → QL申通_网点日质量指标。
    /// 网点级粒度（1 行/网点/日期），本源<b>有网点编码</b>。
    ///
    /// 关键不变量——网点日指标由多个 NetworkMetric 源各填子集、按 网点×日 合并：
    /// 同一 (网点×日) 行会被多个源先后 upsert，每个源<b>只刷新自己负责的字段子集</b>，
    /// <b>绝不把本源不负责的字段清成 null/整行覆盖</b>。本源负责「积压与遗失」子集
    /// （日均出/进港量、积压倍数、超3/5/7天积压量、遗失率ppm/遗失量、进港投诉量/率、虚签投诉率、7日虚签投诉量）；
    /// 出仓/滞留/签收/拦截/渗透等字段留给其它 C3 源填，本源一律不碰（保持既有值）。
    ///
    /// 网点编码（唯一键一部分）：优先用主数据匹配码 <see cref="NetworkMatch.Code"/>；未匹配则回退源 F网点编码 原文，
    /// 保证唯一键稳定且不丢行。日期解析失败的行跳过（计 skip，日志可见）。
    /// 唯一键 (FOrgId,F承运商,F业务日期,F网点编码)，按键 upsert，重复跑不增行（幂等）。
    /// </summary>
    private async global::System.Threading.Tasks.Task<UnifyResult> UnifyBacklogMonitorAsync(
        long orgId, ShentongSourceDescriptor desc, CancellationToken ct)
    {
        // 只投影需要的列（避免读全实体碰 STG 表允许 NULL、实体声明非空 long 的系统列，如 F账套ID NULL → SqlNullValueException）。
        var rows = await _db.Set<StgShentongBacklogMonitor>()
            .IgnoreQueryFilters()
            .Where(r => r.FOrgId == orgId && !r.FIsRevoked)
            .Select(r => new BacklogRow(
                r.FID, r.F批次ID, r.F网点编码, r.F网点名称, r.F统计日期, r.F片区名称, r.F省区,
                r.F日均出港量, r.F日均进港量, r.F积压倍数,
                r.F超3天积压量疑似遗失, r.F超5天积压量智能遗失, r.F超7天积压量超长单,
                r.F遗失率ppm, r.F遗失量,
                r.F进港投诉量, r.F进港投诉率, r.F虚签投诉率上一周, r.F7日虚签投诉量))
            .ToListAsync(ct);

        int netUpserts = 0, networkUnmatched = 0, skipped = 0;

        // 同批去重缓存：键 = (业务日期, 网点编码)。
        // 防止同一次归一里出现两条相同 (网点×日) 行时，第二行查库查不到第一行刚 Add 但未 SaveChanges 的待插实体，
        // 又 Add 第二个 → SaveChangesAsync 撞 DB 唯一索引 → 整批回滚。命中缓存则复用同一实例，
        // 继续走「只刷本源子集不覆盖」的合并逻辑（同批重复键合并为一行）。
        var metricCache = new Dictionary<(DateTime, string), QlShentongNetworkDailyMetric>();

        foreach (var row in rows)
        {
            // ── 网点匹配（本源有编码，按编码解析；无名称传 null）──
            var net = await _matcher.ResolveNetworkAsync(row.F网点编码, null, orgId, ct);
            // 网点编码（唯一键一部分）：匹配到用匹配码；未匹配回退源编码原文，避免丢行。
            //
            // 已知前提（C2 留作前提，不改键结构）：唯一键里「匹配码」与「未匹配回退原文」共用同一
            // F网点编码 命名空间——极端下若某行匹配到的 net.Code 恰与另一行未匹配回退的 F网点编码 原文字面相同，
            // 二者会被当作同一 (网点×日) 行合并。当前积压监控恒带网点编码、基本命中匹配，字面撞码概率极低，
            // 故保留单一命名空间；若 C3 引入码空间不一致的源，需在键上区分「匹配码/原文码」来源。
            var netCode = net.Status == 1 ? net.Code : (row.F网点编码 ?? "");
            if (net.Status == 0) networkUnmatched++;

            // ── 业务日期（本源 F统计日期 为 yyyyMMdd/带分隔均可，ParseUtil.TryDate 已兼容）──
            var bizDate = ParseUtil.TryDate(row.F统计日期);
            if (bizDate == null || string.IsNullOrEmpty(netCode))
            {
                // 日期解析失败 或 无网点编码（连回退原文都空）→ 跳过（无法构成唯一键）。
                skipped++;
                continue;
            }

            // ── 合并 upsert（唯一键：FOrgId × 承运商 × 业务日期 × 网点编码）──
            // 先查同批缓存（命中=本批前面已处理过同键行，复用实例，isNew=false 避免重复 Add）；
            // 未命中再查库；库无则 new+Add+入缓存。
            var cacheKey = (bizDate.Value, netCode!);
            bool isNew = false;
            if (!metricCache.TryGetValue(cacheKey, out var metric))
            {
                var existing = await _db.Set<QlShentongNetworkDailyMetric>()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(m =>
                        m.FOrgId == orgId &&
                        m.F承运商 == "申通" &&
                        m.F业务日期 == bizDate &&
                        m.F网点编码 == netCode, ct);

                isNew = existing == null;
                metric = existing ?? new QlShentongNetworkDailyMetric();
                metricCache[cacheKey] = metric;
            }

            // 键 + 公共维度（多源都可写；名称/片区/省区取本源原文，仅在有值时刷新，避免别源已填被清空）。
            metric.FOrgId = orgId;
            metric.F承运商 = "申通";
            metric.F业务日期 = bizDate.Value;
            metric.F统计年月 = ParseUtil.Ym(bizDate);
            metric.F网点编码 = netCode!;
            if (!string.IsNullOrWhiteSpace(row.F网点名称)) metric.F网点名称 = row.F网点名称;
            if (!string.IsNullOrWhiteSpace(row.F片区名称)) metric.F片区 = row.F片区名称;
            if (!string.IsNullOrWhiteSpace(row.F省区)) metric.F省区 = row.F省区;

            // ── 本源负责子集：积压与遗失（逐列以两实体真实名对齐）──
            // 积压
            metric.F日均出港量 = ParseUtil.TryInt(row.F日均出港量);
            metric.F日均进港量 = ParseUtil.TryInt(row.F日均进港量);
            metric.F积压倍数 = ParseUtil.TryDecimal(row.F积压倍数);
            metric.F超3天积压量 = ParseUtil.TryInt(row.F超3天积压量疑似遗失);
            metric.F超5天积压量 = ParseUtil.TryInt(row.F超5天积压量智能遗失);
            metric.F超7天积压量 = ParseUtil.TryInt(row.F超7天积压量超长单);
            // 遗失
            metric.F遗失率ppm = ParseUtil.TryDecimal(row.F遗失率ppm);
            metric.F遗失量 = ParseUtil.TryInt(row.F遗失量);
            // 进港投诉 / 虚签
            metric.F进港投诉量 = ParseUtil.TryInt(row.F进港投诉量);
            metric.F进港投诉率 = ParseUtil.TryDecimal(row.F进港投诉率);
            metric.F虚签投诉率 = ParseUtil.TryDecimal(row.F虚签投诉率上一周);
            metric.F7日虚签投诉量 = ParseUtil.TryInt(row.F7日虚签投诉量);

            // 关键不变量：出仓/滞留/签收/拦截/渗透等「非本源字段」一律不写——既有值保持不变（不整行覆盖）。

            metric.F来源批次ID = row.F批次ID;

            if (isNew)
            {
                metric.F创建时间 = DateTime.Now;
                _db.Set<QlShentongNetworkDailyMetric>().Add(metric);
            }

            netUpserts++;
        }

        await _db.SaveChangesAsync(ct);

        return new UnifyResult(
            EventsUpserted: 0,
            EmployeeMetricUpserts: 0,
            NetworkMetricUpserts: netUpserts,
            NetworkUnmatched: networkUnmatched,
            EmployeeUnmatched: 0);
    }

    /// <summary>
    /// 积压监控源行的内存投影（只含归一需要的列），避免读全实体碰 NULL 系统列。
    /// </summary>
    private sealed record BacklogRow(
        long FID,
        long F批次ID,
        string? F网点编码,
        string? F网点名称,
        string? F统计日期,
        string? F片区名称,
        string? F省区,
        string? F日均出港量,
        string? F日均进港量,
        string? F积压倍数,
        string? F超3天积压量疑似遗失,
        string? F超5天积压量智能遗失,
        string? F超7天积压量超长单,
        string? F遗失率ppm,
        string? F遗失量,
        string? F进港投诉量,
        string? F进港投诉率,
        string? F虚签投诉率上一周,
        string? F7日虚签投诉量);

    /// <summary>
    /// 按唯一键 (orgId,申通,域,原文) 查问题字典；缺失则创建一条默认条目并保存（让字典从数据自动长出）。
    /// 默认值：编码=前缀+短哈希、名称=原文、严重度=1、不考核、可归责到人、状态=1。
    /// 本次运行内用 cache 去重，确保同批多行同原文只建一条（幂等）。
    /// </summary>
    private async global::System.Threading.Tasks.Task<QlShentongProblemDict> GetOrCreateProblemDictAsync(
        long orgId, ShentongSourceDescriptor desc, string problemRaw,
        Dictionary<string, QlShentongProblemDict> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(problemRaw, out var cached))
            return cached;

        var dict = await _db.Set<QlShentongProblemDict>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d =>
                d.FOrgId == orgId &&
                d.F承运商 == "申通" &&
                d.F质量域 == desc.QualityDomain &&
                d.F来源问题类型原文 == problemRaw, ct);

        if (dict == null)
        {
            dict = new QlShentongProblemDict
            {
                FOrgId = orgId,
                F承运商 = "申通",
                F质量域 = desc.QualityDomain,
                F来源问题类型原文 = problemRaw,
                F问题类型编码 = $"{desc.ProblemCodePrefix}_{ShortHash(desc.QualityDomain, problemRaw)}",
                F问题类型名称 = string.IsNullOrEmpty(problemRaw) ? "(未分类)" : problemRaw,
                F默认严重度 = 1,
                F是否考核 = false,
                F是否可归责到人 = true,
                F状态 = 1,
            };
            _db.Set<QlShentongProblemDict>().Add(dict);
            // 立即保存，保证后续行/重复运行能按唯一键查到（也让多源共用字典一致）
            await _db.SaveChangesAsync(ct);
        }

        cache[problemRaw] = dict;
        return dict;
    }

    /// <summary>
    /// 物流完整性源行的内存投影（只含归一需要的列），避免读全实体碰 NULL 系统列。
    /// </summary>
    private sealed record LogisticsRow(
        long FID,
        long F批次ID,
        string? F统计日期,
        string? F运单号,
        string? F网点名称,
        string? F所属网点名称,
        string? F问题类型,
        string? F订单平台,
        string? F签收员编号,
        string? F签收员名称);

    /// <summary>关键列快照 JSON（用于审计/回溯，存事件的 F关键字段JSON）。</summary>
    private static string BuildKeySnapshot(LogisticsRow row)
    {
        var snap = new Dictionary<string, string?>
        {
            ["F统计日期"] = row.F统计日期,
            ["F运单号"] = row.F运单号,
            ["F网点名称"] = row.F网点名称,
            ["F所属网点名称"] = row.F所属网点名称,
            ["F问题类型"] = row.F问题类型,
            ["F订单平台"] = row.F订单平台,
            ["F签收员编号"] = row.F签收员编号,
            ["F签收员名称"] = row.F签收员名称,
        };
        return JsonSerializer.Serialize(snap);
    }

    /// <summary>
    /// 由「域 + 原文」生成稳定短哈希（8 位十六进制大写），保证同一原文恒得同码（幂等可复现），
    /// 不同原文极低碰撞。编码全局只需在字典唯一键内可辨识，短哈希足够。
    /// </summary>
    private static string ShortHash(string domain, string raw)
    {
        var bytes = Encoding.UTF8.GetBytes($"{domain}|{raw}");
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(8);
        for (int i = 0; i < 4; i++)
            sb.Append(hash[i].ToString("X2"));
        return sb.ToString();
    }
}
