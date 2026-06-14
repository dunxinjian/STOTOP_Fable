using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaCalendarEvent : BaseEntity, IOrgScoped
{
    public string FTitle { get; set; } = string.Empty;
    public string? FDescription { get; set; }
    public string? FLocation { get; set; }
    public DateTime FStartTime { get; set; }
    public DateTime FEndTime { get; set; }
    public DateTime? FActualStartTime { get; set; }
    public DateTime? FActualEndTime { get; set; }
    public int FStatus { get; set; }
    public int FPriority { get; set; }
    public bool FIsAllDay { get; set; }
    public bool FIsRecurring { get; set; }
    public string? FRecurrenceRule { get; set; }
    public DateTime? FRecurrenceEndDate { get; set; }
    public long? FParentEventId { get; set; }
    public long FOrganizerId { get; set; }
    public long FOrgId { get; set; }
    public string? FDingTalkEventId { get; set; }
    public string? FDingTalkCalendarId { get; set; }
    public int FSyncStatus { get; set; }
    public DateTime? FLastSyncTime { get; set; }
    public string? FColor { get; set; }
    public int FRemindMinutes { get; set; }
    public DateTime FCreateTime { get; set; }
    public DateTime FUpdateTime { get; set; }

    // 导航属性
    public virtual ICollection<OaCalendarEventAttendee> Attendees { get; set; } = [];
    public virtual OaCalendarEvent? ParentEvent { get; set; }
    public virtual ICollection<OaCalendarEvent> ChildEvents { get; set; } = [];
}
