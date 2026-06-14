using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/external-payment")]
public class ExternalPaymentController : ControllerBase
{
    private readonly IExternalPaymentService _service;

    public ExternalPaymentController(IExternalPaymentService service)
    {
        _service = service;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("list")]
    public async Task<ApiResult<PagedResult<ExternalPaymentDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] long? orgId = null)
    {
        var result = await _service.GetPagedListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<ExternalPaymentDto>>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResult<ExternalPaymentDto>> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
        {
            return ApiResult<ExternalPaymentDto>.Fail("对外付款单不存在");
        }
        return ApiResult<ExternalPaymentDto>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResult<ExternalPaymentDto>> Create([FromBody] CreateExternalPaymentRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request, GetUserId());
            return ApiResult<ExternalPaymentDto>.Success(result, "创建对外付款单成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<ExternalPaymentDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ApiResult<ExternalPaymentDto>> Update(long id, [FromBody] UpdateExternalPaymentRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, GetUserId());
            if (result == null)
            {
                return ApiResult<ExternalPaymentDto>.Fail("对外付款单不存在");
            }
            return ApiResult<ExternalPaymentDto>.Success(result, "更新对外付款单成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<ExternalPaymentDto>.Fail(ex.Message);
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
                return ApiResult.Fail("对外付款单不存在");
            }
            return ApiResult.Ok("删除对外付款单成功");
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
