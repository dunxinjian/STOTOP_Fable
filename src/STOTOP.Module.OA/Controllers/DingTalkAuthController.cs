using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[ApiController]
[Route("api/auth/dingtalk")]
public class DingTalkAuthController : ControllerBase
{
    private readonly IDingTalkAuthService _service;

    public DingTalkAuthController(IDingTalkAuthService service)
    {
        _service = service;
    }

    /// <summary>
    /// 获取钉钉登录公开配置（无需认证）
    /// </summary>
    [AllowAnonymous]
    [HttpGet("config")]
    public async Task<ApiResult<DingTalkPublicConfigDto>> GetConfig()
    {
        var config = await _service.GetPublicConfigAsync();
        return ApiResult<DingTalkPublicConfigDto>.Success(config);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ApiResult<DingTalkLoginResultDto>> Login([FromBody] DingTalkLoginRequest request)
    {
        try
        {
            var result = await _service.LoginByAuthCodeAsync(request.AuthCode, request.OrgId);
            if (!result.Success)
            {
                return ApiResult<DingTalkLoginResultDto>.Fail(result.ErrorMessage ?? "登录失败");
            }
            return ApiResult<DingTalkLoginResultDto>.Success(result, "登录成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<DingTalkLoginResultDto>.Fail(ex.Message);
        }
    }
}
