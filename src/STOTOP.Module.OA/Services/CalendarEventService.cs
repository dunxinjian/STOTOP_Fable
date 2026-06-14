using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Interfaces;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.OA.Services.Interfaces;
using STOTOP.Module.System.Entities;

namespace STOTOP.Module.OA.Services;

/// <summary>
/// 日历事件服务实现
/// </summary>
public class CalendarEventService : ICalendarEventService
{
    private readonly STOTOPDbContext _db;
    private readonly IRepository<OaCalendarEvent> _eventRepository;
    private readonly IRepository<OaCalendarEventAttendee> _attendeeRepository;
    private readonly IRepository<SysUser> _userRepository;
    private readonly IRepository<SysOrganization> _orgRepository;

    public CalendarEventService(
        STOTOPDbContext db,
        IRepository<OaCalendarEvent> eventRepository,
        IRepository<OaCalendarEventAttendee> attendeeRepository,
        IRepository<SysUser> userRepository,
        IRepository<SysOrganization> orgRepository)
    {
        _db = db;
        _eventRepository = eventRepository;
        _attendeeRepository = attendeeRepository;
        _userRepository = userRepository;
        _orgRepository = orgRepository;
    }

    #region 状态文本映射

    private static string GetStatusText(int status) => status switch
    {
        0 => "未召开",
        1 => "进行中",
        2 => "已召开",
        3 => "提前",
        4 => "延后",
        5 => "取消",
        _ => "未知"
    };

    private static string GetPriorityText(int priority) => priority switch
    {
        0 => "普通",
        1 => "重要",
        2 => "紧急",
        _ => "未知"
    };

    private static string GetResponseStatusText(int responseStatus) => responseStatus switch
    {
        0 => "待回复",
        1 => "接受",
        2 => "拒绝",
        3 => "暂定",
        _ => "未知"
    };

    private static string GetAttendStatusText(int attendStatus) => attendStatus switch
    {
        0 => "未知",
        1 => "已出席",
        2 => "缺席",
        3 => "迟到",
        _ => "未知"
    };

    #endregion

    #region 查询方法

    public async Task<List<CalendarEventDto>> GetListAsync(DateTime startDate, DateTime endDate, long? orgId = null, long? organizerId = null, int? status = null)
    {
        var result = new List<CalendarEventDto>();

        // 1. 查询非周期事件（时间范围内）
        var nonRecurringQuery = _eventRepository.Query()
            .Where(e => !e.FIsRecurring)
            .Where(e => e.FStartTime >= startDate && e.FStartTime < endDate);

        if (orgId.HasValue)
            nonRecurringQuery = nonRecurringQuery.Where(e => e.FOrgId == orgId.Value);

        if (organizerId.HasValue)
            nonRecurringQuery = nonRecurringQuery.Where(e => e.FOrganizerId == organizerId.Value);

        if (status.HasValue)
            nonRecurringQuery = nonRecurringQuery.Where(e => e.FStatus == status.Value);

        var nonRecurringEvents = await nonRecurringQuery.ToListAsync();
        foreach (var evt in nonRecurringEvents)
        {
            result.Add(await MapToDtoAsync(evt));
        }

        // 2. 查询周期事件模板（模板开始时间<=endDate，且未结束或结束时间>=startDate）
        var recurringQuery = _eventRepository.Query()
            .Where(e => e.FIsRecurring)
            .Where(e => e.FStartTime <= endDate)
            .Where(e => !e.FRecurrenceEndDate.HasValue || e.FRecurrenceEndDate.Value >= startDate);

        if (orgId.HasValue)
            recurringQuery = recurringQuery.Where(e => e.FOrgId == orgId.Value);

        if (organizerId.HasValue)
            recurringQuery = recurringQuery.Where(e => e.FOrganizerId == organizerId.Value);

        // 注意：周期事件的状态判断比较复杂，这里不按状态筛选

        var recurringEvents = await recurringQuery.ToListAsync();
        foreach (var template in recurringEvents)
        {
            var instances = await ExpandRecurringEventAsync(template, startDate, endDate);
            result.AddRange(instances);
        }

        return result.OrderBy(e => e.StartTime).ToList();
    }

    public async Task<CalendarBoardDataDto> GetBoardDataAsync(DateTime startDate, DateTime endDate, long? orgId = null)
    {
        var events = await GetListAsync(startDate, endDate, orgId);

        return new CalendarBoardDataDto
        {
            Pending = events.Where(e => e.Status == 0).ToList(),
            InProgress = events.Where(e => e.Status == 1).ToList(),
            Completed = events.Where(e => e.Status == 2).ToList(),
            Early = events.Where(e => e.Status == 3).ToList(),
            Delayed = events.Where(e => e.Status == 4).ToList(),
            Cancelled = events.Where(e => e.Status == 5).ToList()
        };
    }

    public async Task<CalendarEventDto?> GetByIdAsync(long id)
    {
        var evt = await _eventRepository.GetByIdAsync(id);
        return evt == null ? null : await MapToDtoAsync(evt);
    }

    #endregion

    #region 增删改方法

    public async Task<CalendarEventDto> CreateAsync(CreateCalendarEventRequest request, long userId)
    {
        var now = DateTime.Now;

        var evt = new OaCalendarEvent
        {
            FTitle = request.Title,
            FDescription = request.Description,
            FLocation = request.Location,
            FStartTime = request.StartTime,
            FEndTime = request.EndTime,
            FStatus = 0, // 未召开
            FPriority = request.Priority,
            FIsAllDay = request.IsAllDay,
            FIsRecurring = request.IsRecurring,
            FRecurrenceRule = request.RecurrenceRule,
            FRecurrenceEndDate = request.RecurrenceEndDate,
            FOrganizerId = userId,
            FOrgId = request.OrgId,
            FSyncStatus = request.SyncToDingTalk ? 1 : 0, // 1=待同步
            FColor = request.Color,
            FRemindMinutes = request.RemindMinutes,
            FCreateTime = now,
            FUpdateTime = now
        };

        await _eventRepository.AddAsync(evt);

        // 添加组织者为参与者
        var organizerAttendee = new OaCalendarEventAttendee
        {
            FEventId = evt.FID,
            FUserId = userId,
            FResponseStatus = 1, // 接受
            FAttendStatus = 0, // 未知
            FIsRequired = true,
            FCreateTime = now
        };
        await _attendeeRepository.AddAsync(organizerAttendee);

        // 添加其他参与者
        var otherAttendeeIds = request.AttendeeUserIds.Where(id => id != userId).ToList();
        if (otherAttendeeIds.Count > 0)
        {
            foreach (var attendeeId in otherAttendeeIds)
            {
                var attendee = new OaCalendarEventAttendee
                {
                    FEventId = evt.FID,
                    FUserId = attendeeId,
                    FResponseStatus = 0, // 待回复
                    FAttendStatus = 0, // 未知
                    FIsRequired = true,
                    FCreateTime = now
                };
                await _attendeeRepository.AddAsync(attendee);
            }
        }

        return await MapToDtoAsync(evt);
    }

    public async Task<CalendarEventDto?> UpdateAsync(long id, UpdateCalendarEventRequest request, long userId)
    {
        // 使用 AsTracking 确保实体被跟踪（全局 NoTracking 环境）
        var evt = await _db.Set<OaCalendarEvent>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (evt == null)
            return null;

        // 验证权限（只有组织者可以修改）
        if (evt.FOrganizerId != userId)
            throw new UnauthorizedAccessException("只有会议组织者可以修改会议");

        evt.FTitle = request.Title;
        evt.FDescription = request.Description;
        evt.FLocation = request.Location;
        evt.FStartTime = request.StartTime;
        evt.FEndTime = request.EndTime;
        evt.FIsAllDay = request.IsAllDay;
        evt.FPriority = request.Priority;
        evt.FIsRecurring = request.IsRecurring;
        evt.FRecurrenceRule = request.RecurrenceRule;
        evt.FRecurrenceEndDate = request.RecurrenceEndDate;
        evt.FRemindMinutes = request.RemindMinutes;
        evt.FColor = request.Color;
        evt.FUpdateTime = DateTime.Now;

        await _db.SaveChangesAsync();
        return await MapToDtoAsync(evt);
    }

    public async Task<bool> DeleteAsync(long id, long userId)
    {
        var evt = await _db.Set<OaCalendarEvent>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (evt == null)
            return false;

        if (evt.FOrganizerId != userId)
            throw new UnauthorizedAccessException("只有会议组织者可以删除会议");

        // 先删除参与者
        var attendees = await _attendeeRepository.Query()
            .Where(a => a.FEventId == id)
            .ToListAsync();

        foreach (var attendee in attendees)
        {
            await _attendeeRepository.DeleteAsync(attendee.FID);
        }

        await _eventRepository.DeleteAsync(id);
        return true;
    }

    #endregion

    #region 状态操作

    public async Task StartAsync(long id, long userId)
    {
        var evt = await _db.Set<OaCalendarEvent>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (evt == null)
            throw new InvalidOperationException("事件不存在");

        evt.FActualStartTime = DateTime.Now;
        evt.FStatus = 1; // 进行中
        evt.FUpdateTime = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task EndAsync(long id, long userId)
    {
        var evt = await _db.Set<OaCalendarEvent>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (evt == null)
            throw new InvalidOperationException("事件不存在");

        var now = DateTime.Now;
        evt.FActualEndTime = now;
        evt.FUpdateTime = now;

        // 自动判断状态
        if (evt.FActualStartTime.HasValue)
        {
            var diff = (evt.FActualStartTime.Value - evt.FStartTime).TotalMinutes;
            if (diff < -5) // 提前5分钟以上
            {
                evt.FStatus = 3; // 提前
            }
            else if (diff > 5) // 延后5分钟以上
            {
                evt.FStatus = 4; // 延后
            }
            else
            {
                evt.FStatus = 2; // 已召开
            }
        }
        else
        {
            evt.FStatus = 2; // 已召开（没有实际开始时间，默认按正常召开处理）
        }

        await _db.SaveChangesAsync();
    }

    public async Task CancelAsync(long id, long userId)
    {
        var evt = await _db.Set<OaCalendarEvent>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (evt == null)
            throw new InvalidOperationException("事件不存在");

        if (evt.FOrganizerId != userId)
            throw new UnauthorizedAccessException("只有会议组织者可以取消会议");

        evt.FStatus = 5; // 取消
        evt.FUpdateTime = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    #endregion

    #region 参与者操作

    public async Task AddAttendeesAsync(long eventId, List<long> userIds, bool isRequired = true)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId);
        if (evt == null)
            throw new InvalidOperationException("事件不存在");

        // 获取已有的参与者
        var existingAttendees = await _attendeeRepository.Query()
            .Where(a => a.FEventId == eventId)
            .Select(a => a.FUserId)
            .ToListAsync();

        var now = DateTime.Now;
        foreach (var userId in userIds.Where(id => !existingAttendees.Contains(id)))
        {
            var attendee = new OaCalendarEventAttendee
            {
                FEventId = eventId,
                FUserId = userId,
                FResponseStatus = 0, // 待回复
                FAttendStatus = 0, // 未知
                FIsRequired = isRequired,
                FCreateTime = now
            };
            await _attendeeRepository.AddAsync(attendee);
        }
    }

    public async Task RemoveAttendeeAsync(long eventId, long userId)
    {
        var attendee = await _attendeeRepository.Query()
            .FirstOrDefaultAsync(a => a.FEventId == eventId && a.FUserId == userId);

        if (attendee != null)
        {
            await _attendeeRepository.DeleteAsync(attendee.FID);
        }
    }

    public async Task RespondAsync(long eventId, long userId, int responseStatus)
    {
        var attendee = await _db.Set<OaCalendarEventAttendee>()
            .AsTracking()
            .FirstOrDefaultAsync(a => a.FEventId == eventId && a.FUserId == userId);

        if (attendee == null)
            throw new InvalidOperationException("您不是该会议的参与者");

        attendee.FResponseStatus = responseStatus;
        await _db.SaveChangesAsync();
    }

    #endregion

    #region 私有方法

    private async Task<CalendarEventDto> MapToDtoAsync(OaCalendarEvent evt)
    {
        var organizer = await _userRepository.GetByIdAsync(evt.FOrganizerId);
        var org = await _orgRepository.GetByIdAsync(evt.FOrgId);

        // 获取参与者
        var attendees = await _attendeeRepository.Query()
            .Where(a => a.FEventId == evt.FID)
            .ToListAsync();

        var attendeeDtos = new List<CalendarEventAttendeeDto>();
        foreach (var attendee in attendees)
        {
            var user = await _userRepository.GetByIdAsync(attendee.FUserId);
            attendeeDtos.Add(new CalendarEventAttendeeDto
            {
                Id = attendee.FID,
                EventId = attendee.FEventId,
                UserId = attendee.FUserId,
                UserName = user?.FName ?? string.Empty,
                ResponseStatus = attendee.FResponseStatus,
                ResponseStatusText = GetResponseStatusText(attendee.FResponseStatus),
                AttendStatus = attendee.FAttendStatus,
                AttendStatusText = GetAttendStatusText(attendee.FAttendStatus),
                IsRequired = attendee.FIsRequired
            });
        }

        return new CalendarEventDto
        {
            Id = evt.FID,
            Title = evt.FTitle,
            Description = evt.FDescription,
            Location = evt.FLocation,
            StartTime = evt.FStartTime,
            EndTime = evt.FEndTime,
            ActualStartTime = evt.FActualStartTime,
            ActualEndTime = evt.FActualEndTime,
            Status = evt.FStatus,
            StatusText = GetStatusText(evt.FStatus),
            Priority = evt.FPriority,
            PriorityText = GetPriorityText(evt.FPriority),
            IsAllDay = evt.FIsAllDay,
            IsRecurring = evt.FIsRecurring,
            RecurrenceRule = evt.FRecurrenceRule,
            RecurrenceEndDate = evt.FRecurrenceEndDate,
            ParentEventId = evt.FParentEventId,
            OrganizerId = evt.FOrganizerId,
            OrganizerName = organizer?.FName ?? string.Empty,
            OrgId = evt.FOrgId,
            OrgName = org?.FName ?? string.Empty,
            DingTalkEventId = evt.FDingTalkEventId,
            SyncStatus = evt.FSyncStatus,
            LastSyncTime = evt.FLastSyncTime,
            Color = evt.FColor,
            RemindMinutes = evt.FRemindMinutes,
            Attendees = attendeeDtos,
            AttendeeCount = attendeeDtos.Count,
            CreateTime = evt.FCreateTime,
            UpdateTime = evt.FUpdateTime
        };
    }

    /// <summary>
    /// 展开周期性事件，生成指定时间范围内的实例
    /// </summary>
    private async Task<List<CalendarEventDto>> ExpandRecurringEventAsync(OaCalendarEvent template, DateTime startDate, DateTime endDate)
    {
        var result = new List<CalendarEventDto>();

        if (string.IsNullOrEmpty(template.FRecurrenceRule))
        {
            // 没有规则，只返回模板本身（如果在范围内）
            if (template.FStartTime >= startDate && template.FStartTime < endDate)
            {
                result.Add(await MapToDtoWithoutAttendeesAsync(template, template.FStartTime, template.FEndTime));
            }
            return result;
        }

        // 解析 RRULE
        var rule = ParseRecurrenceRule(template.FRecurrenceRule);
        if (rule == null)
            return result;

        var duration = template.FEndTime - template.FStartTime;
        var current = template.FStartTime;
        var endLimit = template.FRecurrenceEndDate ?? endDate;
        var instanceCount = 0;

        while (current < endDate && current <= endLimit && (rule.Count == null || instanceCount < rule.Count.Value))
        {
            if (current >= startDate)
            {
                result.Add(await MapToDtoWithoutAttendeesAsync(template, current, current + duration));
            }

            current = GetNextOccurrence(current, rule);
            instanceCount++;
        }

        return result;
    }

    /// <summary>
    /// 解析 RRULE 字符串
    /// </summary>
    private RecurrenceRule? ParseRecurrenceRule(string rrule)
    {
        try
        {
            var result = new RecurrenceRule();
            var parts = rrule.Split(';');

            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length != 2) continue;

                var key = keyValue[0].ToUpperInvariant();
                var value = keyValue[1];

                switch (key)
                {
                    case "FREQ":
                        result.Freq = value.ToUpperInvariant();
                        break;
                    case "INTERVAL":
                        if (int.TryParse(value, out var interval))
                            result.Interval = interval;
                        break;
                    case "COUNT":
                        if (int.TryParse(value, out var count))
                            result.Count = count;
                        break;
                    case "UNTIL":
                        if (DateTime.TryParse(value, out var until))
                            result.Until = until;
                        break;
                    case "BYDAY":
                        result.ByDay = value.Split(',').ToList();
                        break;
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取下一个周期时间点
    /// </summary>
    private DateTime GetNextOccurrence(DateTime current, RecurrenceRule rule)
    {
        return rule.Freq?.ToUpperInvariant() switch
        {
            "DAILY" => current.AddDays(rule.Interval),
            "WEEKLY" => current.AddDays(7 * rule.Interval),
            "MONTHLY" => current.AddMonths(rule.Interval),
            "YEARLY" => current.AddYears(rule.Interval),
            _ => current.AddDays(1)
        };
    }

    private async Task<CalendarEventDto> MapToDtoWithoutAttendeesAsync(OaCalendarEvent evt, DateTime startTime, DateTime endTime)
    {
        var organizer = await _userRepository.GetByIdAsync(evt.FOrganizerId);
        var org = await _orgRepository.GetByIdAsync(evt.FOrgId);

        return new CalendarEventDto
        {
            Id = evt.FID,
            Title = evt.FTitle,
            Description = evt.FDescription,
            Location = evt.FLocation,
            StartTime = startTime,
            EndTime = endTime,
            ActualStartTime = evt.FActualStartTime,
            ActualEndTime = evt.FActualEndTime,
            Status = evt.FStatus,
            StatusText = GetStatusText(evt.FStatus),
            Priority = evt.FPriority,
            PriorityText = GetPriorityText(evt.FPriority),
            IsAllDay = evt.FIsAllDay,
            IsRecurring = evt.FIsRecurring,
            RecurrenceRule = evt.FRecurrenceRule,
            RecurrenceEndDate = evt.FRecurrenceEndDate,
            ParentEventId = evt.FParentEventId,
            OrganizerId = evt.FOrganizerId,
            OrganizerName = organizer?.FName ?? string.Empty,
            OrgId = evt.FOrgId,
            OrgName = org?.FName ?? string.Empty,
            DingTalkEventId = evt.FDingTalkEventId,
            SyncStatus = evt.FSyncStatus,
            LastSyncTime = evt.FLastSyncTime,
            Color = evt.FColor,
            RemindMinutes = evt.FRemindMinutes,
            Attendees = [],
            AttendeeCount = 0,
            CreateTime = evt.FCreateTime,
            UpdateTime = evt.FUpdateTime
        };
    }

    #endregion

    /// <summary>
    /// 周期规则解析结果
    /// </summary>
    private class RecurrenceRule
    {
        public string? Freq { get; set; }
        public int Interval { get; set; } = 1;
        public int? Count { get; set; }
        public DateTime? Until { get; set; }
        public List<string> ByDay { get; set; } = [];
    }
}
