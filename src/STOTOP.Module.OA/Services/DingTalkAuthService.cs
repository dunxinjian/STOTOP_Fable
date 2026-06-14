using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using STOTOP.Core.Interfaces;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;
using STOTOP.Module.System.Entities;
using STOTOP.Module.System.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace STOTOP.Module.OA.Services;

public class DingTalkAuthService : IDingTalkAuthService
{
    private readonly IRepository<SysUser> _userRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DingTalkAuthService> _logger;

    public DingTalkAuthService(
        IRepository<SysUser> userRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DingTalkAuthService> logger)
    {
        _userRepository = userRepository;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DingTalkLoginResultDto> LoginByAuthCodeAsync(string authCode, long orgId)
    {
        try
        {
            // 1. 获取钉钉全局配置检查是否启用
            var config = DingTalkConfigHelper.GetGlobalConfig();

            if (config == null || config.IsEnabled != 1)
            {
                return new DingTalkLoginResultDto
                {
                    Success = false,
                    ErrorMessage = "系统未配置钉钉集成或未启用"
                };
            }

            // 2. 获取 AccessToken（全局配置）
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new DingTalkLoginResultDto
                {
                    Success = false,
                    ErrorMessage = "获取钉钉 AccessToken 失败"
                };
            }

            // 3. 通过授权码获取用户信息
            var dingTalkUserInfo = await GetUserInfoByAuthCodeAsync(accessToken, authCode);
            if (dingTalkUserInfo == null)
            {
                return new DingTalkLoginResultDto
                {
                    Success = false,
                    ErrorMessage = "获取钉钉用户信息失败"
                };
            }

            // 4. 通过钉钉 unionId 匹配系统用户
            var user = await _userRepository.Query()
                .FirstOrDefaultAsync(u => u.FDingTalkUnionId == dingTalkUserInfo.UnionId);

            if (user == null)
            {
                _logger.LogWarning("钉钉免登失败：钉钉用户 {UnionId} 未绑定系统用户", dingTalkUserInfo.UnionId);
                return new DingTalkLoginResultDto
                {
                    Success = false,
                    ErrorMessage = "该钉钉账号未绑定系统用户，请联系管理员"
                };
            }

            if (user.FStatus != 1)
            {
                return new DingTalkLoginResultDto
                {
                    Success = false,
                    ErrorMessage = "用户已被禁用"
                };
            }

            // 5. 签发 JWT Token
            var token = GenerateJwtToken(user);

            _logger.LogInformation("钉钉免登成功，用户: {UserName} ({UserId})", user.FName, user.FID);
            return new DingTalkLoginResultDto
            {
                Success = true,
                Token = token,
                UserName = user.FName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "钉钉免登认证异常，AuthCode: {AuthCode}, OrgId: {OrgId}", authCode, orgId);
            return new DingTalkLoginResultDto
            {
                Success = false,
                ErrorMessage = "钉钉认证服务异常，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 通过钉钉临时授权码获取用户信息
    /// </summary>
    private async Task<DingTalkUserInfo?> GetUserInfoByAuthCodeAsync(string accessToken, string authCode)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();

            // 先通过授权码获取用户 userid
            var url = $"https://oapi.dingtalk.com/topapi/v2/user/getuserinfo?access_token={accessToken}";
            var requestBody = new { code = authCode };
            var response = await client.PostAsJsonAsync(url, requestBody);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取钉钉用户信息失败，HTTP {StatusCode}: {Content}", response.StatusCode, content);
                return null;
            }

            var json = JsonDocument.Parse(content);
            var errcode = json.RootElement.GetProperty("errcode").GetInt32();
            if (errcode != 0)
            {
                var errmsg = json.RootElement.GetProperty("errmsg").GetString();
                _logger.LogWarning("获取钉钉用户信息失败: {ErrMsg}", errmsg);
                return null;
            }

            var result = json.RootElement.GetProperty("result");
            var userid = result.GetProperty("userid").GetString();

            // 再通过 userid 获取用户详情（含 unionId）
            var detailUrl = $"https://oapi.dingtalk.com/topapi/v2/user/get?access_token={accessToken}";
            var detailRequest = new { userid };
            var detailResponse = await client.PostAsJsonAsync(detailUrl, detailRequest);
            var detailContent = await detailResponse.Content.ReadAsStringAsync();

            if (!detailResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取钉钉用户详情失败，HTTP {StatusCode}: {Content}", detailResponse.StatusCode, detailContent);
                return null;
            }

            var detailJson = JsonDocument.Parse(detailContent);
            var detailErrcode = detailJson.RootElement.GetProperty("errcode").GetInt32();
            if (detailErrcode != 0)
            {
                var errmsg = detailJson.RootElement.GetProperty("errmsg").GetString();
                _logger.LogWarning("获取钉钉用户详情失败: {ErrMsg}", errmsg);
                return null;
            }

            var detailResult = detailJson.RootElement.GetProperty("result");
            return new DingTalkUserInfo
            {
                UserId = detailResult.GetProperty("userid").GetString() ?? string.Empty,
                UnionId = detailResult.GetProperty("unionid").GetString() ?? string.Empty,
                Name = detailResult.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取钉钉用户信息异常");
            return null;
        }
    }

    /// <summary>
    /// 签发 STOTOP JWT Token
    /// </summary>
    private string GenerateJwtToken(SysUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expireMinutes = int.Parse(jwtSettings["ExpireMinutes"] ?? "480");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.FID.ToString()),
            new Claim(ClaimTypes.Name, user.FAccount),
            new Claim("userName", user.FName),
            new Claim("userId", user.FID.ToString()),
            new Claim("loginType", "dingtalk")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 获取钉钉登录公开配置（无需认证）
    /// </summary>
    public Task<DingTalkPublicConfigDto> GetPublicConfigAsync()
    {
        // 获取全局配置（F组织ID IS NULL）
        var config = DingTalkConfigHelper.GetGlobalConfig();

        // 如果没有全局配置或未启用，返回 enabled = false
        if (config == null || config.IsEnabled != 1)
        {
            return Task.FromResult(new DingTalkPublicConfigDto { Enabled = false });
        }

        // 构建回调地址：{系统域名}/login?dingtalk=callback
        var redirectUri = !string.IsNullOrEmpty(config.Domain)
            ? $"{config.Domain.TrimEnd('/')}/login?dingtalk=callback"
            : null;

        return Task.FromResult(new DingTalkPublicConfigDto
        {
            AppKey = config.AppKey,
            CorpId = config.CorpId,
            RedirectUri = redirectUri,
            Enabled = true
            // 注意：绝不暴露 AppSecret
        });
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var config = DingTalkConfigHelper.GetGlobalConfig()
            ?? throw new InvalidOperationException("钉钉配置不存在，请先配置钉钉应用信息");

        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(config.AppKey))
            missingFields.Add("AppKey");
        if (string.IsNullOrWhiteSpace(config.AppSecret))
            missingFields.Add("AppSecret");
        if (string.IsNullOrWhiteSpace(config.CorpId))
            missingFields.Add("CorpId");

        if (missingFields.Count > 0)
            throw new InvalidOperationException($"钉钉配置缺少必填字段: {string.Join(", ", missingFields)}，请完善配置");

        using var client = _httpClientFactory.CreateClient();
        var appSecret = DingTalkConfigHelper.DecryptSecret(config.AppSecret);
        if (string.IsNullOrEmpty(appSecret))
            appSecret = config.AppSecret;

        var body = new { appKey = config.AppKey, appSecret };
        var response = await client.PostAsJsonAsync("https://api.dingtalk.com/v1.0/oauth2/accessToken", body);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("获取钉钉 AccessToken 失败：返回值为空");
    }

    /// <summary>
    /// 钉钉用户信息内部类
    /// </summary>
    private class DingTalkUserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UnionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
