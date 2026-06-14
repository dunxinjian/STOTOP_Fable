using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/petty-cash")]
public class PettyCashController : ControllerBase
{
    private readonly IPettyCashService _service;

    public PettyCashController(IPettyCashService service)
    {
        _service = service;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    // ===== 备用金申请 =====

    [HttpGet("apply/list")]
    public async Task<ApiResult<PagedResult<PettyCashApplyDto>>> GetApplyList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null, [FromQuery] long? orgId = null)
    {
        var result = await _service.GetApplyListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<PettyCashApplyDto>>.Success(result);
    }

    [HttpGet("apply/{id}")]
    public async Task<ApiResult<PettyCashApplyDto>> GetApplyById(long id)
    {
        var result = await _service.GetApplyByIdAsync(id);
        if (result == null)
        {
            return ApiResult<PettyCashApplyDto>.Fail("备用金申请单不存在");
        }
        return ApiResult<PettyCashApplyDto>.Success(result);
    }

    [HttpPost("apply")]
    public async Task<ApiResult<PettyCashApplyDto>> CreateApply([FromBody] CreatePettyCashApplyRequest request)
    {
        try
        {
            var result = await _service.CreateApplyAsync(request, GetUserId());
            return ApiResult<PettyCashApplyDto>.Success(result, "创建备用金申请成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<PettyCashApplyDto>.Fail(ex.Message);
        }
    }

    [HttpPut("apply/{id}")]
    public async Task<ApiResult<PettyCashApplyDto>> UpdateApply(long id, [FromBody] UpdatePettyCashApplyRequest request)
    {
        try
        {
            var result = await _service.UpdateApplyAsync(id, request, GetUserId());
            if (result == null)
            {
                return ApiResult<PettyCashApplyDto>.Fail("备用金申请单不存在");
            }
            return ApiResult<PettyCashApplyDto>.Success(result, "更新备用金申请成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<PettyCashApplyDto>.Fail(ex.Message);
        }
    }

    [HttpPost("apply/{id}/submit")]
    public async Task<ApiResult> SubmitApply(long id)
    {
        try
        {
            await _service.SubmitApplyAsync(id, GetUserId());
            return ApiResult.Ok("提交审批成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    // ===== 备用金报销 =====

    [HttpGet("reimburse/list")]
    public async Task<ApiResult<PagedResult<PettyCashReimburseDto>>> GetReimburseList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null, [FromQuery] long? orgId = null)
    {
        var result = await _service.GetReimburseListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<PettyCashReimburseDto>>.Success(result);
    }

    [HttpPost("reimburse")]
    public async Task<ApiResult<PettyCashReimburseDto>> CreateReimburse([FromBody] CreatePettyCashReimburseRequest request)
    {
        try
        {
            var result = await _service.CreateReimburseAsync(request, GetUserId());
            return ApiResult<PettyCashReimburseDto>.Success(result, "创建备用金报销成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<PettyCashReimburseDto>.Fail(ex.Message);
        }
    }

    [HttpPost("reimburse/{id}/submit")]
    public async Task<ApiResult> SubmitReimburse(long id)
    {
        try
        {
            await _service.SubmitReimburseAsync(id, GetUserId());
            return ApiResult.Ok("提交审批成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    // ===== 备用金还款 =====

    [HttpGet("return/list")]
    public async Task<ApiResult<PagedResult<PettyCashReturnDto>>> GetReturnList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null, [FromQuery] long? orgId = null)
    {
        var result = await _service.GetReturnListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<PettyCashReturnDto>>.Success(result);
    }

    [HttpPost("return")]
    public async Task<ApiResult<PettyCashReturnDto>> CreateReturn([FromBody] CreatePettyCashReturnRequest request)
    {
        try
        {
            var result = await _service.CreateReturnAsync(request, GetUserId());
            return ApiResult<PettyCashReturnDto>.Success(result, "创建备用金还款成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<PettyCashReturnDto>.Fail(ex.Message);
        }
    }

    [HttpPost("return/{id}/submit")]
    public async Task<ApiResult> SubmitReturn(long id)
    {
        try
        {
            await _service.SubmitReturnAsync(id, GetUserId());
            return ApiResult.Ok("提交审批成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    // ===== 备用金冲销 =====

    [HttpGet("writeoff/list")]
    public async Task<ApiResult<PagedResult<PettyCashWriteOffDto>>> GetWriteOffList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null, [FromQuery] long? orgId = null)
    {
        var result = await _service.GetWriteOffListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<PettyCashWriteOffDto>>.Success(result);
    }

    [HttpPost("writeoff")]
    public async Task<ApiResult<PettyCashWriteOffDto>> CreateWriteOff([FromBody] CreatePettyCashWriteOffRequest request)
    {
        try
        {
            var result = await _service.CreateWriteOffAsync(request, GetUserId());
            return ApiResult<PettyCashWriteOffDto>.Success(result, "创建备用金冲销成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<PettyCashWriteOffDto>.Fail(ex.Message);
        }
    }

    [HttpPost("writeoff/{id}/submit")]
    public async Task<ApiResult> SubmitWriteOff(long id)
    {
        try
        {
            await _service.SubmitWriteOffAsync(id, GetUserId());
            return ApiResult.Ok("提交审批成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    // ===== 备用金台账 =====

    [HttpGet("ledger")]
    public async Task<ApiResult<List<PettyCashLedgerDto>>> GetLedger(
        [FromQuery] long? orgId = null,
        [FromQuery] long? applicantId = null)
    {
        var result = await _service.GetLedgerAsync(orgId, applicantId);
        return ApiResult<List<PettyCashLedgerDto>>.Success(result);
    }
}
