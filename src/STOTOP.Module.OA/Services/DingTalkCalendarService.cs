using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.OA.Services.Interfaces;
using STOTOP.Module.System.Entities;
using STOTOP.Module.System.Services.Interfaces;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace STOTOP.Module.OA.Services;

public class DingTalkCalendarService : IDingTalkCalendarService
{
    private readonly STOTOPDbContext _context;
    private readonly IDingTalkService _dingTalkService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DingTalkCalendarService> _logger;

    public DingTalkCalendarService(
        STOTOPDbContext context,
        IDingTalkService dingTalkService,
        IHttpClientFactory httpClientFactory,
        ILogger<DingTalkCalendarService> logger)
    {
        _context = context;
        _dingTalkService = dingTalkService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 推送本地日程到钉钉
    /// </summary>
    public async Task<string?> PushEventToDingTalkAsync(long eventId)
    {
        try
        {
            // 1. 从数据库加载事件（含参与者）
            var calendarEvent = await _context.Set<OaCalendarEvent>()
                .AsTracking()
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.FID == eventId);

            if (calendarEvent == null)
            {
                _logger.LogWarning("推送日程到钉钉失败：事件 {EventId} 不存在", eventId);
                throw new InvalidOperationException($"事件 {eventId} 不存在");
            }

            // 2. 获取钉钉 AccessToken
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("推送日程到钉钉失败：无法获取 AccessToken");
                throw new InvalidOperationException("无法获取钉钉 AccessToken");
            }

            // 3. 获取组织者的钉钉 unionId
            var organizer = await _context.Set<SysUser>()
                .FirstOrDefaultAsync(u => u.FID == calendarEvent.FOrganizerId);

            if (organizer == null || string.IsNullOrEmpty(organizer.FDingTalkUnionId))
            {
                _logger.LogWarning("推送日程到钉钉失败：组织者 {OrganizerId} 不存在或未绑定钉钉", calendarEvent.FOrganizerId);
                throw new InvalidOperationException($"组织者 {calendarEvent.FOrganizerId} 不存在或未绑定钉钉");
            }

            var unionId = organizer.FDingTalkUnionId;

            // 4. 构建参与者列表
            var attendees = new List<object>();
            if (calendarEvent.Attendees != null && calendarEvent.Attendees.Any())
            {
                var attendeeUserIds = calendarEvent.Attendees.Select(a => a.FUserId).ToList();
                var attendeeUsers = await _context.Set<SysUser>()
                    .Where(u => attendeeUserIds.Contains(u.FID) && !string.IsNullOrEmpty(u.FDingTalkUnionId))
                    .ToListAsync();

                foreach (var user in attendeeUsers)
                {
                    var attendeeInfo = calendarEvent.Attendees.FirstOrDefault(a => a.FUserId == user.FID);
                    attendees.Add(new
                    {
                        id = user.FDingTalkUnionId,
                        isOptional = attendeeInfo?.FIsRequired == false
                    });
                }
            }

            // 5. 构建请求体
            object requestBody;
            if (calendarEvent.FIsAllDay)
            {
                // 全天事件使用 date 字段
                requestBody = new
                {
                    summary = calendarEvent.FTitle,
                    description = calendarEvent.FDescription,
                    start = new { date = calendarEvent.FStartTime.ToString("yyyy-MM-dd"), timeZone = "Asia/Shanghai" },
                    end = new { date = calendarEvent.FEndTime.ToString("yyyy-MM-dd"), timeZone = "Asia/Shanghai" },
                    isAllDay = true,
                    location = string.IsNullOrEmpty(calendarEvent.FLocation) ? null : new { displayName = calendarEvent.FLocation },
                    attendees = attendees.Any() ? attendees : null,
                    reminders = calendarEvent.FRemindMinutes > 0
                        ? new[] { new { method = "dingtalk", minutes = calendarEvent.FRemindMinutes } }
                        : null
                };
            }
            else
            {
                // 非全天事件使用 dateTime 字段
                requestBody = new
                {
                    summary = calendarEvent.FTitle,
                    description = calendarEvent.FDescription,
                    start = new { dateTime = calendarEvent.FStartTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), timeZone = "Asia/Shanghai" },
                    end = new { dateTime = calendarEvent.FEndTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), timeZone = "Asia/Shanghai" },
                    isAllDay = false,
                    location = string.IsNullOrEmpty(calendarEvent.FLocation) ? null : new { displayName = calendarEvent.FLocation },
                    attendees = attendees.Any() ? attendees : null,
                    reminders = calendarEvent.FRemindMinutes > 0
                        ? new[] { new { method = "dingtalk", minutes = calendarEvent.FRemindMinutes } }
                        : null
                };
            }

            // 6. 调用钉钉日历 API
            var url = $"https://api.dingtalk.com/v1.0/calendar/users/{unionId}/calendars/primary/events";

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-acs-dingtalk-access-token", accessToken);

            var response = await client.PostAsJsonAsync(url, requestBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("推送日程到钉钉失败，HTTP {StatusCode}: {Response}", response.StatusCode, responseContent);
                throw new InvalidOperationException($"推送日程到钉钉失败: HTTP {(int)response.StatusCode}");
            }

            // 7. 解析响应，获取钉钉事件ID
            var json = JsonDocument.Parse(responseContent);
            if (json.RootElement.TryGetProperty("id", out var idElement))
            {
                var dingTalkEventId = idElement.GetString();
                
                // 8. 保存返回的钉钉事件ID到本地记录
                calendarEvent.FDingTalkEventId = dingTalkEventId;
                calendarEvent.FSyncStatus = 1;
                calendarEvent.FLastSyncTime = DateTime.Now;
                calendarEvent.FUpdateTime = DateTime.Now;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("推送日程到钉钉成功，EventId: {EventId}, DingTalkEventId: {DingTalkEventId}", 
                    eventId, dingTalkEventId);
                return dingTalkEventId;
            }

            _logger.LogWarning("推送日程到钉钉：响应中未包含事件ID，Response: {Response}", responseContent);
            throw new InvalidOperationException("钉钉 API 响应中未包含事件ID");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送日程到钉钉失败，EventId: {EventId}", eventId);
            throw;
        }
    }

    /// <summary>
    /// 从钉钉拉取日程到本地
    /// </summary>
    public async Task<int> PullEventsFromDingTalkAsync(DateTime startTime, DateTime endTime)
    {
        try
        {
            // 1. 获取 AccessToken
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("从钉钉拉取日程失败：无法获取 AccessToken");
                throw new InvalidOperationException("无法获取钉钉 AccessToken");
            }

            // 获取所有已绑定钉钉的用户
            var users = await _context.Set<SysUser>()
                .Where(u => !string.IsNullOrEmpty(u.FDingTalkUnionId))
                .ToListAsync();

            if (!users.Any())
            {
                _logger.LogWarning("从钉钉拉取日程失败：没有已绑定钉钉的用户");
                return 0;
            }

            var syncCount = 0;
            var timeMin = startTime.ToString("yyyy-MM-ddTHH:mm:sszzz");
            var timeMax = endTime.ToString("yyyy-MM-ddTHH:mm:sszzz");

            foreach (var user in users)
            {
                var unionId = user.FDingTalkUnionId!;
                var url = $"https://api.dingtalk.com/v1.0/calendar/users/{unionId}/calendars/primary/events?timeMin={Uri.EscapeDataString(timeMin)}&timeMax={Uri.EscapeDataString(timeMax)}";

                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-acs-dingtalk-access-token", accessToken);

                var response = await client.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("从钉钉拉取日程失败，User: {UserId}, HTTP {StatusCode}: {Response}", 
                        user.FID, response.StatusCode, responseContent);
                    continue;
                }

                var json = JsonDocument.Parse(responseContent);
                if (!json.RootElement.TryGetProperty("events", out var eventsElement))
                {
                    continue;
                }

                foreach (var eventItem in eventsElement.EnumerateArray())
                {
                    var dingTalkEventId = eventItem.GetProperty("id").GetString();
                    if (string.IsNullOrEmpty(dingTalkEventId)) continue;

                    // 通过 eventId 匹配本地记录
                    var existingEvent = await _context.Set<OaCalendarEvent>()
                        .AsTracking()
                        .FirstOrDefaultAsync(e => e.FDingTalkEventId == dingTalkEventId);

                    var summary = eventItem.TryGetProperty("summary", out var summaryProp) ? summaryProp.GetString() : string.Empty;
                    var description = eventItem.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
                    var isAllDay = eventItem.TryGetProperty("isAllDay", out var allDayProp) && allDayProp.GetBoolean();

                    DateTime startDateTime;
                    DateTime endDateTime;

                    if (isAllDay)
                    {
                        // 全天事件
                        if (eventItem.TryGetProperty("start", out var startProp) && startProp.TryGetProperty("date", out var startDateProp))
                        {
                            startDateTime = DateTime.Parse(startDateProp.GetString()!);
                        }
                        else
                        {
                            continue;
                        }

                        if (eventItem.TryGetProperty("end", out var endProp) && endProp.TryGetProperty("date", out var endDateProp))
                        {
                            endDateTime = DateTime.Parse(endDateProp.GetString()!);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // 非全天事件
                        if (eventItem.TryGetProperty("start", out var startProp) && startProp.TryGetProperty("dateTime", out var startDateTimeProp))
                        {
                            startDateTime = DateTime.Parse(startDateTimeProp.GetString()!);
                        }
                        else
                        {
                            continue;
                        }

                        if (eventItem.TryGetProperty("end", out var endProp) && endProp.TryGetProperty("dateTime", out var endDateTimeProp))
                        {
                            endDateTime = DateTime.Parse(endDateTimeProp.GetString()!);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    string? location = null;
                    if (eventItem.TryGetProperty("location", out var locationProp) && locationProp.TryGetProperty("displayName", out var locationNameProp))
                    {
                        location = locationNameProp.GetString();
                    }

                    if (existingEvent != null)
                    {
                        // 更新现有记录
                        existingEvent.FTitle = summary ?? existingEvent.FTitle;
                        existingEvent.FDescription = description ?? existingEvent.FDescription;
                        existingEvent.FStartTime = startDateTime;
                        existingEvent.FEndTime = endDateTime;
                        existingEvent.FIsAllDay = isAllDay;
                        existingEvent.FLocation = location ?? existingEvent.FLocation;
                        existingEvent.FSyncStatus = 1;
                        existingEvent.FLastSyncTime = DateTime.Now;
                        existingEvent.FUpdateTime = DateTime.Now;
                    }
                    else
                    {
                        // 创建新的本地记录
                        var newEvent = new OaCalendarEvent
                        {
                            FTitle = summary ?? "钉钉日程",
                            FDescription = description,
                            FStartTime = startDateTime,
                            FEndTime = endDateTime,
                            FIsAllDay = isAllDay,
                            FLocation = location,
                            FOrganizerId = user.FID,
                            FOrgId = 0, // 默认组织ID
                            FDingTalkEventId = dingTalkEventId,
                            FDingTalkCalendarId = "primary",
                            FSyncStatus = 1,
                            FLastSyncTime = DateTime.Now,
                            FStatus = 0,
                            FPriority = 0,
                            FRemindMinutes = 15,
                            FCreateTime = DateTime.Now,
                            FUpdateTime = DateTime.Now
                        };
                        await _context.Set<OaCalendarEvent>().AddAsync(newEvent);
                    }

                    syncCount++;
                }

                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("从钉钉拉取日程完成，共同步 {SyncCount} 条记录", syncCount);
            return syncCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从钉钉拉取日程失败");
            throw;
        }
    }

    /// <summary>
    /// 删除钉钉上的日程
    /// </summary>
    public async Task DeleteDingTalkEventAsync(string dingTalkEventId)
    {
        try
        {
            if (string.IsNullOrEmpty(dingTalkEventId))
            {
                throw new ArgumentException("钉钉事件ID不能为空", nameof(dingTalkEventId));
            }

            // 1. 获取 AccessToken
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("删除钉钉日程失败：无法获取 AccessToken");
                throw new InvalidOperationException("无法获取钉钉 AccessToken");
            }

            // 2. 查找本地记录获取组织者信息
            var calendarEvent = await _context.Set<OaCalendarEvent>()
                .FirstOrDefaultAsync(e => e.FDingTalkEventId == dingTalkEventId);

            if (calendarEvent == null)
            {
                _logger.LogWarning("删除钉钉日程失败：本地未找到事件 {DingTalkEventId}", dingTalkEventId);
                throw new InvalidOperationException($"本地未找到钉钉事件 {dingTalkEventId}");
            }

            // 3. 获取组织者的钉钉 unionId
            var organizer = await _context.Set<SysUser>()
                .FirstOrDefaultAsync(u => u.FID == calendarEvent.FOrganizerId);

            if (organizer == null || string.IsNullOrEmpty(organizer.FDingTalkUnionId))
            {
                _logger.LogWarning("删除钉钉日程失败：组织者 {OrganizerId} 不存在或未绑定钉钉", calendarEvent.FOrganizerId);
                throw new InvalidOperationException($"组织者 {calendarEvent.FOrganizerId} 不存在或未绑定钉钉");
            }

            var unionId = organizer.FDingTalkUnionId;

            // 4. 调用钉钉 API 删除日程
            var url = $"https://api.dingtalk.com/v1.0/calendar/users/{unionId}/calendars/primary/events/{dingTalkEventId}";

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-acs-dingtalk-access-token", accessToken);

            var response = await client.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("删除钉钉日程成功，DingTalkEventId: {DingTalkEventId}", dingTalkEventId);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError("删除钉钉日程失败，HTTP {StatusCode}: {Response}", response.StatusCode, content);
                throw new InvalidOperationException($"删除钉钉日程失败: HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除钉钉日程失败，DingTalkEventId: {DingTalkEventId}", dingTalkEventId);
            throw;
        }
    }

    /// <summary>
    /// 更新钉钉上的日程
    /// </summary>
    public async Task UpdateDingTalkEventAsync(long eventId)
    {
        try
        {
            // 1. 加载本地事件
            var calendarEvent = await _context.Set<OaCalendarEvent>()
                .AsTracking()
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.FID == eventId);

            if (calendarEvent == null)
            {
                _logger.LogWarning("更新钉钉日程失败：事件 {EventId} 不存在", eventId);
                throw new InvalidOperationException($"事件 {eventId} 不存在");
            }

            if (string.IsNullOrEmpty(calendarEvent.FDingTalkEventId))
            {
                _logger.LogWarning("更新钉钉日程失败：事件 {EventId} 未同步到钉钉", eventId);
                throw new InvalidOperationException($"事件 {eventId} 未同步到钉钉");
            }

            // 2. 获取钉钉 AccessToken
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("更新钉钉日程失败：无法获取 AccessToken");
                throw new InvalidOperationException("无法获取钉钉 AccessToken");
            }

            // 3. 获取组织者的钉钉 unionId
            var organizer = await _context.Set<SysUser>()
                .FirstOrDefaultAsync(u => u.FID == calendarEvent.FOrganizerId);

            if (organizer == null || string.IsNullOrEmpty(organizer.FDingTalkUnionId))
            {
                _logger.LogWarning("更新钉钉日程失败：组织者 {OrganizerId} 不存在或未绑定钉钉", calendarEvent.FOrganizerId);
                throw new InvalidOperationException($"组织者 {calendarEvent.FOrganizerId} 不存在或未绑定钉钉");
            }

            var unionId = organizer.FDingTalkUnionId;

            // 4. 构建参与者列表
            var attendees = new List<object>();
            if (calendarEvent.Attendees != null && calendarEvent.Attendees.Any())
            {
                var attendeeUserIds = calendarEvent.Attendees.Select(a => a.FUserId).ToList();
                var attendeeUsers = await _context.Set<SysUser>()
                    .Where(u => attendeeUserIds.Contains(u.FID) && !string.IsNullOrEmpty(u.FDingTalkUnionId))
                    .ToListAsync();

                foreach (var user in attendeeUsers)
                {
                    var attendeeInfo = calendarEvent.Attendees.FirstOrDefault(a => a.FUserId == user.FID);
                    attendees.Add(new
                    {
                        id = user.FDingTalkUnionId,
                        isOptional = attendeeInfo?.FIsRequired == false
                    });
                }
            }

            // 5. 构建请求体
            object requestBody;
            if (calendarEvent.FIsAllDay)
            {
                requestBody = new
                {
                    summary = calendarEvent.FTitle,
                    description = calendarEvent.FDescription,
                    start = new { date = calendarEvent.FStartTime.ToString("yyyy-MM-dd"), timeZone = "Asia/Shanghai" },
                    end = new { date = calendarEvent.FEndTime.ToString("yyyy-MM-dd"), timeZone = "Asia/Shanghai" },
                    isAllDay = true,
                    location = string.IsNullOrEmpty(calendarEvent.FLocation) ? null : new { displayName = calendarEvent.FLocation },
                    attendees = attendees.Any() ? attendees : null,
                    reminders = calendarEvent.FRemindMinutes > 0
                        ? new[] { new { method = "dingtalk", minutes = calendarEvent.FRemindMinutes } }
                        : null
                };
            }
            else
            {
                requestBody = new
                {
                    summary = calendarEvent.FTitle,
                    description = calendarEvent.FDescription,
                    start = new { dateTime = calendarEvent.FStartTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), timeZone = "Asia/Shanghai" },
                    end = new { dateTime = calendarEvent.FEndTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), timeZone = "Asia/Shanghai" },
                    isAllDay = false,
                    location = string.IsNullOrEmpty(calendarEvent.FLocation) ? null : new { displayName = calendarEvent.FLocation },
                    attendees = attendees.Any() ? attendees : null,
                    reminders = calendarEvent.FRemindMinutes > 0
                        ? new[] { new { method = "dingtalk", minutes = calendarEvent.FRemindMinutes } }
                        : null
                };
            }

            // 6. 调用钉钉 API 更新日程
            var url = $"https://api.dingtalk.com/v1.0/calendar/users/{unionId}/calendars/primary/events/{calendarEvent.FDingTalkEventId}";

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-acs-dingtalk-access-token", accessToken);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                // 更新本地同步状态
                calendarEvent.FSyncStatus = 1;
                calendarEvent.FLastSyncTime = DateTime.Now;
                calendarEvent.FUpdateTime = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新钉钉日程成功，EventId: {EventId}, DingTalkEventId: {DingTalkEventId}", 
                    eventId, calendarEvent.FDingTalkEventId);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError("更新钉钉日程失败，HTTP {StatusCode}: {Response}", response.StatusCode, content);
                throw new InvalidOperationException($"更新钉钉日程失败: HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新钉钉日程失败，EventId: {EventId}", eventId);
            throw;
        }
    }

    /// <summary>
    /// 获取钉钉 AccessToken（使用全局配置）
    /// </summary>
    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await _dingTalkService.GetAccessTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取钉钉 AccessToken 失败");
            return null;
        }
    }
}
