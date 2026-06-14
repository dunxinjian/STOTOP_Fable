namespace STOTOP.Module.OA.Services.Interfaces;

public interface IDingTalkCalendarService
{
    /// <summary>推送本地日程到钉钉</summary>
    Task<string?> PushEventToDingTalkAsync(long eventId);
    
    /// <summary>从钉钉拉取日程到本地</summary>
    Task<int> PullEventsFromDingTalkAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>删除钉钉上的日程</summary>
    Task DeleteDingTalkEventAsync(string dingTalkEventId);
    
    /// <summary>更新钉钉上的日程</summary>
    Task UpdateDingTalkEventAsync(long eventId);
}
