using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.Quality.Dtos.CarrierDashboard;
using STOTOP.Module.Quality.Services.CarrierDashboard;
using STOTOP.Module.System.Filters;

namespace STOTOP.Module.Quality.Controllers;

[Authorize]
[ApiController]
[Route("api/quality/carrier-dashboard")]
public class CarrierQualityDashboardController : ControllerBase
{
    private readonly ICarrierQualityDashboardService _service;

    public CarrierQualityDashboardController(ICarrierQualityDashboardService service)
    {
        _service = service;
    }

    private long GetOrgId() => (long)(HttpContext.Items["CurrentOrgId"] ?? 0L);

    // ── 视图1 网点总览 ──
    [HttpGet("network/kpi")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<NetworkKpiDto>> GetNetworkKpi([FromQuery] string carrier, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? networkCode)
        => _service.GetNetworkKpiAsync(GetOrgId(), carrier, from, to, networkCode);

    [HttpGet("network/trend")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<List<NetworkTrendPointDto>>> GetNetworkTrend([FromQuery] string carrier, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? networkCode)
        => _service.GetNetworkTrendAsync(GetOrgId(), carrier, from, to, networkCode);

    [HttpGet("network/domain-distribution")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<List<DomainStatItem>>> GetDomainDistribution([FromQuery] string carrier, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? networkCode)
        => _service.GetDomainDistributionAsync(GetOrgId(), carrier, from, to, networkCode);

    [HttpGet("network/fee-by-domain")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<List<DomainStatItem>>> GetFeeByDomain([FromQuery] string carrier, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? networkCode)
        => _service.GetFeeByDomainAsync(GetOrgId(), carrier, from, to, networkCode);

    // ── 视图2 员工质量 ──
    [HttpGet("employee/rank")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<EmployeeRankDto>> GetEmployeeRank([FromQuery] string carrier, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? networkCode, [FromQuery] string dimension = "problem", [FromQuery] int topN = 10)
        => _service.GetEmployeeRankAsync(GetOrgId(), carrier, from, to, networkCode, dimension, topN);

    [HttpGet("employee/metrics")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<EmployeeMetricsPageDto>> GetEmployeeMetrics([FromQuery] string carrier, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? networkCode, [FromQuery] int page = 1, [FromQuery] int size = 50)
        => _service.GetEmployeeMetricsAsync(GetOrgId(), carrier, from, to, networkCode, page, size);

    [HttpGet("employee/{empNo}/timeline")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<List<EmployeeEventItemDto>>> GetEmployeeTimeline([FromRoute] string empNo, [FromQuery] string carrier, [FromQuery] DateTime from, [FromQuery] DateTime to)
        => _service.GetEmployeeTimelineAsync(GetOrgId(), carrier, empNo, from, to);

    // ── 视图3 问题件追踪 ──
    [HttpGet("events")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<EventPageDto>> GetEvents([FromQuery] EventQuery query)
        => _service.GetEventsAsync(GetOrgId(), query);

    [HttpGet("pending-count")]
    [RequirePermission(QualityPermissions.CarrierQualityView)]
    public Task<ApiResult<int>> GetPendingCount([FromQuery] string carrier)
        => _service.GetPendingCountAsync(GetOrgId(), carrier);
}
