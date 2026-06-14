using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IDingTalkAuthService
{
    /// <summary>
    /// 通过钉钉临时授权码获取用户信息并签发JWT
    /// </summary>
    /// <param name="authCode">钉钉临时授权码</param>
    /// <param name="orgId">组织ID，0表示使用全局配置</param>
    Task<DingTalkLoginResultDto> LoginByAuthCodeAsync(string authCode, long orgId);

    /// <summary>
    /// 获取钉钉登录公开配置（无需认证）
    /// </summary>
    /// <returns>公开配置信息，不包含AppSecret等敏感信息</returns>
    Task<DingTalkPublicConfigDto> GetPublicConfigAsync();
}
