namespace STOTOP.Module.OA.Dtos;

/// <summary>
/// 钉钉登录公开配置（供前端使用，不含敏感信息）
/// </summary>
public class DingTalkPublicConfigDto
{
    /// <summary>
    /// 钉钉应用 AppKey
    /// </summary>
    public string? AppKey { get; set; }

    /// <summary>
    /// 企业 CorpId
    /// </summary>
    public string? CorpId { get; set; }

    /// <summary>
    /// 回调地址：{系统域名}/login?dingtalk=callback
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// 是否已启用钉钉登录
    /// </summary>
    public bool Enabled { get; set; }
}
