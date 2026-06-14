namespace STOTOP.Module.OA.Dtos;

/// <summary>
/// 日历事件响应DTO
/// </summary>
public class CalendarEventDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string PriorityText { get; set; } = string.Empty;
    public bool IsAllDay { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public long? ParentEventId { get; set; }
    public long OrganizerId { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public string? DingTalkEventId { get; set; }
    public int SyncStatus { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public string? Color { get; set; }
    public int RemindMinutes { get; set; }
    public List<CalendarEventAttendeeDto> Attendees { get; set; } = [];
    public int AttendeeCount { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

/// <summary>
/// 会议参与者DTO
/// </summary>
public class CalendarEventAttendeeDto
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ResponseStatus { get; set; }
    public string ResponseStatusText { get; set; } = string.Empty;
    public int AttendStatus { get; set; }
    public string AttendStatusText { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

/// <summary>
/// 创建日历事件请求
/// </summary>
public class CreateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAllDay { get; set; }
    public int Priority { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public long OrgId { get; set; }
    public List<long> AttendeeUserIds { get; set; } = [];
    public int RemindMinutes { get; set; }
    public string? Color { get; set; }
    public bool SyncToDingTalk { get; set; }
}

/// <summary>
/// 更新日历事件请求
/// </summary>
public class UpdateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAllDay { get; set; }
    public int Priority { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public int RemindMinutes { get; set; }
    public string? Color { get; set; }
}

/// <summary>
/// 日历看板数据DTO
/// </summary>
public class CalendarBoardDataDto
{
    public List<CalendarEventDto> Pending { get; set; } = [];
    public List<CalendarEventDto> InProgress { get; set; } = [];
    public List<CalendarEventDto> Completed { get; set; } = [];
    public List<CalendarEventDto> Early { get; set; } = [];
    public List<CalendarEventDto> Delayed { get; set; } = [];
    public List<CalendarEventDto> Cancelled { get; set; } = [];
}
