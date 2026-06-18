using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Models;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.Quality.Dtos.CarrierDashboard;
using STOTOP.Module.Quality.Entities;

namespace STOTOP.Module.Quality.Services.CarrierDashboard;

public class CarrierQualityDashboardService : ICarrierQualityDashboardService
{
    private readonly STOTOPDbContext _db;

    public CarrierQualityDashboardService(STOTOPDbContext db)
    {
        _db = db;
    }

    // ───────────────────────── 纯函数口径（可单测）─────────────────────────

    /// <summary>件量加权平均：Σ(value*weight)/Σweight；权重和为0或全空时退算术平均；无有效值返回 null。</summary>
    public static decimal? WeightedAverage(IEnumerable<(decimal? Value, decimal? Weight)> items)
    {
        decimal sumVW = 0m, sumW = 0m, sumV = 0m;
        int n = 0;
        foreach (var (v, w) in items)
        {
            if (v is null) continue;
            n++;
            sumV += v.Value;
            var weight = w ?? 0m;
            sumVW += v.Value * weight;
            sumW += weight;
        }
        if (n == 0) return null;
        return sumW > 0m ? sumVW / sumW : sumV / n;
    }

    /// <summary>期间累计：忽略 null 求和。</summary>
    public static decimal PeriodSum(IEnumerable<decimal?> values)
    {
        decimal s = 0m;
        foreach (var v in values) if (v.HasValue) s += v.Value;
        return s;
    }

    /// <summary>派送超时合计：T0..T3 求和，null 当 0。</summary>
    public static int TimeoutTotal(int? t0, int? t1, int? t2, int? t3)
        => (t0 ?? 0) + (t1 ?? 0) + (t2 ?? 0) + (t3 ?? 0);

    // ───────────────────────── 视图方法（Phase 1-3 实现）─────────────────────────
    // 占位：先抛 NotImplementedException，使接口可编译、DI 可注册；逐 Phase 替换为真实现。

    public async Task<ApiResult<NetworkKpiDto>> GetNetworkKpiAsync(long orgId, string carrier, DateTime from, DateTime to, string? networkCode)
    {
        var toEnd = to.Date.AddDays(1); // 半开右界，含 to 当天
        var q = _db.Set<QlShentongNetworkDailyMetric>()
            .Where(m => m.FOrgId == orgId && m.F承运商 == carrier
                        && m.F业务日期 >= from.Date && m.F业务日期 < toEnd);
        if (!string.IsNullOrEmpty(networkCode))
            q = q.Where(m => m.F网点编码 == networkCode);

        var rows = await q.ToListAsync(); // 跨期合成在内存做（避免 EF 翻译末日/加权）
        var dto = new NetworkKpiDto();
        if (rows.Count > 0)
        {
            var lastDay = rows.Max(r => r.F业务日期).Date;
            var lastRows = rows.Where(r => r.F业务日期.Date == lastDay).ToList();
            // 率值=末日快照，多网点件量加权（权重 F日均出港量）
            decimal? Snap(Func<QlShentongNetworkDailyMetric, decimal?> sel)
                => WeightedAverage(lastRows.Select(r => (sel(r), (decimal?)r.F日均出港量)));

            dto.SignRateToday = Snap(r => r.F当天及时签收率);
            dto.SignRate48h = Snap(r => r.F48h签收率);
            dto.OutboundOnTimeRate = Snap(r => r.F一频次出仓及时率);
            dto.RetentionRate = Snap(r => r.F滞留率);
            dto.BacklogMultiple = Snap(r => r.F积压倍数);
            dto.LossRatePpm = Snap(r => r.F遗失率ppm);
            dto.FakeSignRate = Snap(r => r.F虚签投诉率);
            // 金额=期间累计
            dto.TotalAssessFee = PeriodSum(rows.Select(r => r.F考核金额合计));
        }

        // 问题件数=期间事件计数
        var evq = _db.Set<QlShentongQualityEvent>()
            .Where(e => e.FOrgId == orgId && e.F承运商 == carrier
                        && e.F业务日期 >= from.Date && e.F业务日期 < toEnd);
        if (!string.IsNullOrEmpty(networkCode))
            evq = evq.Where(e => e.F网点编码 == networkCode);
        dto.ProblemEventCount = await evq.CountAsync();

        return ApiResult<NetworkKpiDto>.Success(dto);
    }

    public async Task<ApiResult<List<NetworkTrendPointDto>>> GetNetworkTrendAsync(long orgId, string carrier, DateTime from, DateTime to, string? networkCode)
    {
        var toEnd = to.Date.AddDays(1);
        var q = _db.Set<QlShentongNetworkDailyMetric>()
            .Where(m => m.FOrgId == orgId && m.F承运商 == carrier
                        && m.F业务日期 >= from.Date && m.F业务日期 < toEnd);
        if (!string.IsNullOrEmpty(networkCode))
            q = q.Where(m => m.F网点编码 == networkCode);

        var rows = await q.ToListAsync();
        var result = rows
            .GroupBy(r => r.F业务日期.Date)
            .OrderBy(g => g.Key)
            .Select(g => new NetworkTrendPointDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                SignRateToday = WeightedAverage(g.Select(r => (r.F当天及时签收率, (decimal?)r.F日均出港量))),
                OutboundOnTimeRate = WeightedAverage(g.Select(r => (r.F一频次出仓及时率, (decimal?)r.F日均出港量))),
                RetentionRate = WeightedAverage(g.Select(r => (r.F滞留率, (decimal?)r.F日均出港量))),
                FakeSignRate = WeightedAverage(g.Select(r => (r.F虚签投诉率, (decimal?)r.F日均出港量))),
            })
            .ToList();

        return ApiResult<List<NetworkTrendPointDto>>.Success(result);
    }
    public async Task<ApiResult<List<DomainStatItem>>> GetDomainDistributionAsync(long orgId, string carrier, DateTime from, DateTime to, string? networkCode)
    {
        var toEnd = to.Date.AddDays(1);
        var q = _db.Set<QlShentongQualityEvent>()
            .Where(e => e.FOrgId == orgId && e.F承运商 == carrier
                        && e.F业务日期 >= from.Date && e.F业务日期 < toEnd);
        if (!string.IsNullOrEmpty(networkCode))
            q = q.Where(e => e.F网点编码 == networkCode);

        var data = await q
            .GroupBy(e => e.F质量域)
            .Select(g => new { Domain = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = data
            .OrderByDescending(d => d.Count)
            .Select(d => new DomainStatItem { Domain = d.Domain, Count = d.Count })
            .ToList();

        return ApiResult<List<DomainStatItem>>.Success(result);
    }

    public async Task<ApiResult<List<DomainStatItem>>> GetFeeByDomainAsync(long orgId, string carrier, DateTime from, DateTime to, string? networkCode)
    {
        var toEnd = to.Date.AddDays(1);
        var q = _db.Set<QlShentongNetworkDailyMetric>()
            .Where(m => m.FOrgId == orgId && m.F承运商 == carrier
                        && m.F业务日期 >= from.Date && m.F业务日期 < toEnd);
        if (!string.IsNullOrEmpty(networkCode))
            q = q.Where(m => m.F网点编码 == networkCode);

        var rows = await q.ToListAsync();
        var result = new List<DomainStatItem>
        {
            new() { Domain = "出仓", Fee = PeriodSum(rows.Select(r => r.F出仓预估考核金额)) },
            new() { Domain = "滞留", Fee = PeriodSum(rows.Select(r => r.F滞留预估考核金额)) },
            new() { Domain = "派送", Fee = PeriodSum(rows.Select(r => r.F派送预估考核金额)) },
            new() { Domain = "签收", Fee = PeriodSum(rows.Select(r => r.F签收率考核金额)) },
        };
        result = result.Where(x => x.Fee != 0m).OrderByDescending(x => x.Fee).ToList();

        return ApiResult<List<DomainStatItem>>.Success(result);
    }
    public Task<ApiResult<EmployeeRankDto>> GetEmployeeRankAsync(long orgId, string carrier, DateTime from, DateTime to, string? networkCode, string dimension, int topN) => throw new NotImplementedException();
    public Task<ApiResult<EmployeeMetricsPageDto>> GetEmployeeMetricsAsync(long orgId, string carrier, DateTime from, DateTime to, string? networkCode, int page, int size) => throw new NotImplementedException();
    public Task<ApiResult<List<EmployeeEventItemDto>>> GetEmployeeTimelineAsync(long orgId, string carrier, string empNo, DateTime from, DateTime to) => throw new NotImplementedException();
    public Task<ApiResult<EventPageDto>> GetEventsAsync(long orgId, EventQuery query) => throw new NotImplementedException();
    public Task<ApiResult<int>> GetPendingCountAsync(long orgId, string carrier) => throw new NotImplementedException();
}
