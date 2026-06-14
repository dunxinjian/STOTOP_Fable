using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/salary-advance")]
public class SalaryAdvanceController : ControllerBase
{
    private readonly ISalaryAdvanceService _service;

    public SalaryAdvanceController(ISalaryAdvanceService service)
    {
        _service = service;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("list")]
    public async Task<ApiResult<PagedResult<SalaryAdvanceDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] long? orgId = null)
    {
        var result = await _service.GetPagedListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<SalaryAdvanceDto>>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResult<SalaryAdvanceDto>> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
        {
            return ApiResult<SalaryAdvanceDto>.Fail("预支工资单不存在");
        }
        return ApiResult<SalaryAdvanceDto>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResult<SalaryAdvanceDto>> Create([FromBody] CreateSalaryAdvanceRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request, GetUserId());
            return ApiResult<SalaryAdvanceDto>.Success(result, "创建预支工资申请成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<SalaryAdvanceDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ApiResult<SalaryAdvanceDto>> Update(long id, [FromBody] UpdateSalaryAdvanceRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, GetUserId());
            if (result == null)
            {
                return ApiResult<SalaryAdvanceDto>.Fail("预支工资单不存在");
            }
            return ApiResult<SalaryAdvanceDto>.Success(result, "更新预支工资申请成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<SalaryAdvanceDto>.Fail(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ApiResult> Delete(long id)
    {
        try
        {
            var result = await _service.DeleteAsync(id, GetUserId());
            if (!result)
            {
                return ApiResult.Fail("预支工资单不存在");
            }
            return ApiResult.Ok("删除预支工资申请成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    [HttpPost("{id}/submit")]
    public async Task<ApiResult> Submit(long id)
    {
        try
        {
            await _service.SubmitAsync(id, GetUserId());
            return ApiResult.Ok("提交审批成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }
}
