using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/expense-account-mapping")]
public class ExpenseAccountMappingController : ControllerBase
{
    private readonly IExpenseAccountMappingService _service;

    public ExpenseAccountMappingController(IExpenseAccountMappingService service)
    {
        _service = service;
    }

    [HttpGet("list")]
    public async Task<ApiResult<List<ExpenseAccountMappingDto>>> GetList(
        [FromQuery] long? expenseTypeId = null,
        [FromQuery] long? orgId = null)
    {
        var result = await _service.GetListAsync(expenseTypeId, orgId);
        return ApiResult<List<ExpenseAccountMappingDto>>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResult<ExpenseAccountMappingDto>> Create([FromBody] CreateExpenseAccountMappingRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return ApiResult<ExpenseAccountMappingDto>.Success(result, "创建映射成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<ExpenseAccountMappingDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ApiResult<ExpenseAccountMappingDto>> Update(long id, [FromBody] UpdateExpenseAccountMappingRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (result == null)
        {
            return ApiResult<ExpenseAccountMappingDto>.Fail("映射不存在");
        }
        return ApiResult<ExpenseAccountMappingDto>.Success(result, "更新映射成功");
    }

    [HttpDelete("{id}")]
    public async Task<ApiResult> Delete(long id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
        {
            return ApiResult.Fail("映射不存在");
        }
        return ApiResult.Ok("删除映射成功");
    }
}
