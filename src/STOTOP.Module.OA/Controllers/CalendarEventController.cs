using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/calendar-event")]
public class CalendarEventController : ControllerBase
{
    private readonly ICalendarEventService _service;
    private readonly IDingTalkCalendarService _dingTalkService;

    public CalendarEventController(
        ICalendarEventService service,
        IDingTalkCalendarService dingTalkService)
    {
        _service = service;
        _dingTalkService = dingTalkService;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    /// <summary>
    /// 日历视图查询
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResult<List<CalendarEventDto>>> GetList(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] long? orgId = null,
        [FromQuery] long? organizerId = null,
        [FromQuery] int? status = null)
    {
        var result = await _service.GetListAsync(startDate, endDate, orgId, organizerId, status);
        return ApiResult<List<CalendarEventDto>>.Success(result);
    }

    /// <summary>
    /// 看板视图查询
    /// </summary>
    [HttpGet("board")]
    public async Task<ApiResult<CalendarBoardDataDto>> GetBoard(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] long? orgId = null)
    {
        var result = await _service.GetBoardDataAsync(startDate, endDate, orgId);
        return ApiResult<CalendarBoardDataDto>.Success(result);
    }

    /// <summary>
    /// 日程详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResult<CalendarEventDto>> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
        {
            return ApiResult<CalendarEventDto>.Fail("日程不存在");
        }
        return ApiResult<CalendarEventDto>.Success(result);
    }

    /// <summary>
    /// 创建日程
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<CalendarEventDto>> Create([FromBody] CreateCalendarEventRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request, GetUserId());
            return ApiResult<CalendarEventDto>.Success(result, "创建日程成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<CalendarEventDto>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 更新日程
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResult<CalendarEventDto>> Update(long id, [FromBody] UpdateCalendarEventRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, GetUserId());
            if (result == null)
            {
                return ApiResult<CalendarEventDto>.Fail("日程不存在");
            }
            return ApiResult<CalendarEventDto>.Success(result, "更新日程成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<CalendarEventDto>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 删除日程
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResult> Delete(long id)
    {
        try
        {
            var result = await _service.DeleteAsync(id, GetUserId());
            if (!result)
            {
                return ApiResult.Fail("日程不存在");
            }
            return ApiResult.Ok("删除日程成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 开始会议
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<ApiResult> Start(long id)
    {
        try
        {
            await _service.StartAsync(id, GetUserId());
            return ApiResult.Ok("会议已开始");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 结束会议
    /// </summary>
    [HttpPost("{id}/end")]
    public async Task<ApiResult> End(long id)
    {
        try
        {
            await _service.EndAsync(id, GetUserId());
            return ApiResult.Ok("会议已结束");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 取消会议
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ApiResult> Cancel(long id)
    {
        try
        {
            await _service.CancelAsync(id, GetUserId());
            return ApiResult.Ok("会议已取消");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 添加参与者
    /// </summary>
    [HttpPost("{id}/attendees")]
    public async Task<ApiResult> AddAttendees(long id, [FromBody] AddAttendeesRequest request)
    {
        try
        {
            await _service.AddAttendeesAsync(id, request.UserIds, request.IsRequired);
            return ApiResult.Ok("添加参与者成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 移除参与者
    /// </summary>
    [HttpDelete("{id}/attendees/{userId}")]
    public async Task<ApiResult> RemoveAttendee(long id, long userId)
    {
        try
        {
            await _service.RemoveAttendeeAsync(id, userId);
            return ApiResult.Ok("移除参与者成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 参与者回复
    /// </summary>
    [HttpPut("{id}/attendees/{userId}/response")]
    public async Task<ApiResult> Respond(long id, long userId, [FromBody] RespondRequest request)
    {
        try
        {
            await _service.RespondAsync(id, userId, request.ResponseStatus);
            return ApiResult.Ok("回复成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 推送日程到钉钉
    /// </summary>
    /// <param name="request">推送请求，包含要推送的事件ID列表</param>
    [HttpPost("sync/push")]
    public async Task<ApiResult<SyncPushResultDto>> SyncPush([FromBody] SyncPushRequest? request = null)
    {
        try
        {
            var results = new List<SyncPushItemResult>();
            
            // 如果指定了事件ID，则只推送指定的事件
            // 否则推送所有未同步的事件
            var eventIds = request?.EventIds;
            
            if (eventIds == null || eventIds.Count == 0)
            {
                // 获取所有未同步的事件
                var today = DateTime.Today;
                var events = await _service.GetListAsync(today, today.AddMonths(1), null, null, null);
                eventIds = events.Where(e => e.SyncStatus == 0).Select(e => e.Id).ToList();
            }
            
            foreach (var eventId in eventIds)
            {
                try
                {
                    var dingTalkEventId = await _dingTalkService.PushEventToDingTalkAsync(eventId);
                    results.Add(new SyncPushItemResult
                    {
                        EventId = eventId,
                        Success = true,
                        DingTalkEventId = dingTalkEventId
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new SyncPushItemResult
                    {
                        EventId = eventId,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }
            
            return ApiResult<SyncPushResultDto>.Success(new SyncPushResultDto
            {
                TotalCount = results.Count,
                SuccessCount = results.Count(r => r.Success),
                FailedCount = results.Count(r => !r.Success),
                Results = results
            });
        }
        catch (Exception ex)
        {
            return ApiResult<SyncPushResultDto>.Fail($"推送失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从钉钉拉取日程
    /// </summary>
    /// <param name="request">拉取请求，包含时间范围</param>
    [HttpPost("sync/pull")]
    public async Task<ApiResult<SyncPullResultDto>> SyncPull([FromBody] SyncPullRequest? request = null)
    {
        try
        {
            var startTime = request?.StartTime ?? DateTime.Today.AddDays(-7);
            var endTime = request?.EndTime ?? DateTime.Today.AddDays(30);
            
            var count = await _dingTalkService.PullEventsFromDingTalkAsync(startTime, endTime);
            
            return ApiResult<SyncPullResultDto>.Success(new SyncPullResultDto
            {
                StartTime = startTime,
                EndTime = endTime,
                PulledCount = count
            });
        }
        catch (Exception ex)
        {
            return ApiResult<SyncPullResultDto>.Fail($"拉取失败: {ex.Message}");
        }
    }
}

/// <summary>
/// 同步推送请求
/// </summary>
public class SyncPushRequest
{
    /// <summary>
    /// 要推送的事件ID列表，为空则推送所有未同步的事件
    /// </summary>
    public List<long>? EventIds { get; set; }
}

/// <summary>
/// 同步推送结果
/// </summary>
public class SyncPushResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<SyncPushItemResult> Results { get; set; } = [];
}

/// <summary>
/// 单个事件推送结果
/// </summary>
public class SyncPushItemResult
{
    public long EventId { get; set; }
    public bool Success { get; set; }
    public string? DingTalkEventId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 同步拉取请求
/// </summary>
public class SyncPullRequest
{
    /// <summary>
    /// 开始时间，默认为一周前
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间，默认为30天后
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 同步拉取结果
/// </summary>
public class SyncPullResultDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int PulledCount { get; set; }
}

/// <summary>
/// 添加参与者请求
/// </summary>
public class AddAttendeesRequest
{
    public List<long> UserIds { get; set; } = [];
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// 参与者回复请求
/// </summary>
public class RespondRequest
{
    public int ResponseStatus { get; set; }
}
