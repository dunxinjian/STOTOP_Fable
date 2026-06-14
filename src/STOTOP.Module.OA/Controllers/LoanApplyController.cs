using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/loan-apply")]
public class LoanApplyController : ControllerBase
{
    private readonly ILoanApplyService _service;

    public LoanApplyController(ILoanApplyService service)
    {
        _service = service;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("list")]
    public async Task<ApiResult<PagedResult<LoanApplyDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] long? orgId = null)
    {
        var result = await _service.GetPagedListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<LoanApplyDto>>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResult<LoanApplyDto>> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
        {
            return ApiResult<LoanApplyDto>.Fail("借款申请单不存在");
        }
        return ApiResult<LoanApplyDto>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResult<LoanApplyDto>> Create([FromBody] CreateLoanApplyRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request, GetUserId());
            return ApiResult<LoanApplyDto>.Success(result, "创建借款申请成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<LoanApplyDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ApiResult<LoanApplyDto>> Update(long id, [FromBody] UpdateLoanApplyRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, GetUserId());
            if (result == null)
            {
                return ApiResult<LoanApplyDto>.Fail("借款申请单不存在");
            }
            return ApiResult<LoanApplyDto>.Success(result, "更新借款申请成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<LoanApplyDto>.Fail(ex.Message);
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
                return ApiResult.Fail("借款申请单不存在");
            }
            return ApiResult.Ok("删除借款申请成功");
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

    [HttpGet("ledger")]
    public async Task<ApiResult<List<LoanLedgerDto>>> GetLedger(
        [FromQuery] long? orgId = null,
        [FromQuery] long? applicantId = null)
    {
        var result = await _service.GetLedgerAsync(orgId, applicantId);
        return ApiResult<List<LoanLedgerDto>>.Success(result);
    }
}
