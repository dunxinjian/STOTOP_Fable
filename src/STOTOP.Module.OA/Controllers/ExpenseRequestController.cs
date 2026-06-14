using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/expense-request")]
public class ExpenseRequestController : ControllerBase
{
    private readonly IExpenseRequestService _service;

    public ExpenseRequestController(IExpenseRequestService service)
    {
        _service = service;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("list")]
    public async Task<ApiResult<PagedResult<ExpenseRequestDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] long? orgId = null)
    {
        var result = await _service.GetPagedListAsync(GetUserId(), page, pageSize, status, orgId);
        return ApiResult<PagedResult<ExpenseRequestDto>>.Success(result);
    }

    [HttpGet("{id}")]
    public async Task<ApiResult<ExpenseRequestDto>> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
        {
            return ApiResult<ExpenseRequestDto>.Fail("费用请款单不存在");
        }
        return ApiResult<ExpenseRequestDto>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResult<ExpenseRequestDto>> Create([FromBody] CreateExpenseRequestRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request, GetUserId());
            return ApiResult<ExpenseRequestDto>.Success(result, "创建费用请款单成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<ExpenseRequestDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ApiResult<ExpenseRequestDto>> Update(long id, [FromBody] UpdateExpenseRequestRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, GetUserId());
            if (result == null)
            {
                return ApiResult<ExpenseRequestDto>.Fail("费用请款单不存在");
            }
            return ApiResult<ExpenseRequestDto>.Success(result, "更新费用请款单成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<ExpenseRequestDto>.Fail(ex.Message);
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
                return ApiResult.Fail("费用请款单不存在");
            }
            return ApiResult.Ok("删除费用请款单成功");
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
