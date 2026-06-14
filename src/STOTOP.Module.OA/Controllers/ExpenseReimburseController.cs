using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/expense-reimburse")]
public class ExpenseReimburseController : ControllerBase
{
    private readonly IExpenseReimburseService _service;

    public ExpenseReimburseController(IExpenseReimburseService service)
    {
        _service = service;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("list")]
    public async Task<ApiResult<PagedResult<ExpenseReimburseDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] long? orgId = null)
    {
        var result = await _service.GetPagedListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<ExpenseReimburseDto>>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResult<ExpenseReimburseDto>> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
        {
            return ApiResult<ExpenseReimburseDto>.Fail("费用报销单不存在");
        }
        return ApiResult<ExpenseReimburseDto>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResult<ExpenseReimburseDto>> Create([FromBody] CreateExpenseReimburseRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request, GetUserId());
            return ApiResult<ExpenseReimburseDto>.Success(result, "创建费用报销单成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<ExpenseReimburseDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ApiResult<ExpenseReimburseDto>> Update(long id, [FromBody] UpdateExpenseReimburseRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, GetUserId());
            if (result == null)
            {
                return ApiResult<ExpenseReimburseDto>.Fail("费用报销单不存在");
            }
            return ApiResult<ExpenseReimburseDto>.Success(result, "更新费用报销单成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<ExpenseReimburseDto>.Fail(ex.Message);
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
                return ApiResult.Fail("费用报销单不存在");
            }
            return ApiResult.Ok("删除费用报销单成功");
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

    [HttpGet("available-requests")]
    public async Task<ApiResult<List<AvailableRequestDto>>> GetAvailableRequests([FromQuery] long orgId)
    {
        var result = await _service.GetAvailableRequestsAsync(orgId, GetUserId());
        return ApiResult<List<AvailableRequestDto>>.Success(result);
    }

    [HttpGet("available-loans")]
    public async Task<ApiResult<List<AvailableLoanDto>>> GetAvailableLoans([FromQuery] long orgId)
    {
        var result = await _service.GetAvailableLoansAsync(orgId, GetUserId());
        return ApiResult<List<AvailableLoanDto>>.Success(result);
    }
}
