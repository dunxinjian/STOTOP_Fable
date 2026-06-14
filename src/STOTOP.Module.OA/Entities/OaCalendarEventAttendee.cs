using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaCalendarEventAttendee : BaseEntity
{
    public long FEventId { get; set; }
    public long FUserId { get; set; }
    public int FResponseStatus { get; set; }
    public int FAttendStatus { get; set; }
    public bool FIsRequired { get; set; }
    public DateTime FCreateTime { get; set; }

    // 导航属性
    public virtual OaCalendarEvent Event { get; set; } = null!;
}
