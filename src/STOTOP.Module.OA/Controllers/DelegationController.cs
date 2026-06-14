using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/delegation")]
public class DelegationController : ControllerBase
{
    private readonly IDelegationService _service;

    public DelegationController(IDelegationService service)
    {
        _service = service;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("list")]
    public async Task<ApiResult<List<DelegationDto>>> GetList()
    {
        var result = await _service.GetMyDelegationsAsync(GetUserId());
        return ApiResult<List<DelegationDto>>.Success(result);
    }

    [HttpPost]
    public async Task<ApiResult<DelegationDto>> Create([FromBody] CreateDelegationRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return ApiResult<DelegationDto>.Success(result, "创建委托成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<DelegationDto>.Fail(ex.Message);
        }
    }

    [HttpPost("{id}/revoke")]
    public async Task<ApiResult> Revoke(long id)
    {
        try
        {
            var result = await _service.RevokeDelegationAsync(id, GetUserId());
            if (!result)
            {
                return ApiResult.Fail("委托不存在");
            }
            return ApiResult.Ok("撤销委托成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }
}
