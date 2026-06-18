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
        // C0：仅事件类（物流完整性明细）。后续 C1/C2/C3 按 ShentongSourceMap 分发其它源。
        var desc = ShentongSourceMap.All[ShentongSourceMap.LogisticsCompletenessTable];
        return await UnifyLogisticsCompletenessAsync(orgId, desc, ct);
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
