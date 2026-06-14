using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/expense-type")]
public class ExpenseTypeController : ControllerBase
{
    private readonly IExpenseTypeService _service;

    public ExpenseTypeController(IExpenseTypeService service)
    {
        _service = service;
    }

    [HttpGet("list")]
    public async Task<ApiResult<List<ExpenseTypeDto>>> GetList(
        [FromQuery] long? orgId = null,
        [FromQuery] string? scene = null)
    {
        var result = await _service.GetListAsync(orgId, scene);
        return ApiResult<List<ExpenseTypeDto>>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResult<ExpenseTypeDto>> Create([FromBody] CreateExpenseTypeRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return ApiResult<ExpenseTypeDto>.Success(result, "创建费用类型成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<ExpenseTypeDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ApiResult<ExpenseTypeDto>> Update(long id, [FromBody] UpdateExpenseTypeRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (result == null)
        {
            return ApiResult<ExpenseTypeDto>.Fail("费用类型不存在");
        }
        return ApiResult<ExpenseTypeDto>.Success(result, "更新费用类型成功");
    }

    [HttpDelete("{id}")]
    public async Task<ApiResult> Delete(long id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
            {
                return ApiResult.Fail("费用类型不存在");
            }
            return ApiResult.Ok("删除费用类型成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    [HttpPost("{id}/toggle")]
    public async Task<ApiResult> Toggle(long id)
    {
        var result = await _service.ToggleAsync(id);
        if (!result)
        {
            return ApiResult.Fail("费用类型不存在");
        }
        return ApiResult.Ok("状态切换成功");
    }
}
