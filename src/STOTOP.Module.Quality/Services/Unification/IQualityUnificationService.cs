namespace STOTOP.Module.Quality.Services.Unification;

/// <summary>
/// 归一一次的结果计数。
/// </summary>
/// <param name="EventsUpserted">写入/更新的质量事件行数。</param>
/// <param name="EmployeeMetricUpserts">写入/更新的员工日指标行数（C0 暂为 0）。</param>
/// <param name="NetworkMetricUpserts">写入/更新的网点日指标行数（C0 暂为 0）。</param>
/// <param name="NetworkUnmatched">网点未匹配（Status==0）的事件数。</param>
/// <param name="EmployeeUnmatched">员工未确定绑定（Status∈{0,3}）的事件数。</param>
public record UnifyResult(
    int EventsUpserted,
    int EmployeeMetricUpserts,
    int NetworkMetricUpserts,
    int NetworkUnmatched,
    int EmployeeUnmatched);

/// <summary>
/// 申通归一服务：把各 STG 来源明细归一为统一质控的事实表（质量事件 / 网点·员工日指标）。
/// 全程显式带 orgId，主数据匹配（网点/员工脏名）落地，幂等（按唯一键 upsert）。
/// </summary>
public interface IQualityUnificationService
{
    /// <summary>
    /// 归一指定组织下的申通各源。C0 仅实现事件类（物流完整性明细）路径。
    /// </summary>
    global::System.Threading.Tasks.Task<UnifyResult> UnifyShentongAsync(long orgId, CancellationToken ct = default);

    // RematchUnresolvedAsync 留 C4：仅重跑未匹配/启发式候选的主数据匹配，不重建事件。
}
