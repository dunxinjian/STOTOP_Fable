using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

/// <summary>
/// 日历事件服务接口
/// </summary>
public interface ICalendarEventService
{
    /// <summary>
    /// 获取事件列表（支持时间范围、组织筛选）
    /// </summary>
    Task<List<CalendarEventDto>> GetListAsync(DateTime startDate, DateTime endDate, long? orgId = null, long? organizerId = null, int? status = null);

    /// <summary>
    /// 获取看板数据（按状态分组）
    /// </summary>
    Task<CalendarBoardDataDto> GetBoardDataAsync(DateTime startDate, DateTime endDate, long? orgId = null);

    /// <summary>
    /// 根据ID获取事件详情
    /// </summary>
    Task<CalendarEventDto?> GetByIdAsync(long id);

    /// <summary>
    /// 创建事件
    /// </summary>
    Task<CalendarEventDto> CreateAsync(CreateCalendarEventRequest request, long userId);

    /// <summary>
    /// 更新事件
    /// </summary>
    Task<CalendarEventDto?> UpdateAsync(long id, UpdateCalendarEventRequest request, long userId);

    /// <summary>
    /// 删除事件
    /// </summary>
    Task<bool> DeleteAsync(long id, long userId);

    /// <summary>
    /// 开始事件
    /// </summary>
    Task StartAsync(long id, long userId);

    /// <summary>
    /// 结束事件
    /// </summary>
    Task EndAsync(long id, long userId);

    /// <summary>
    /// 取消事件
    /// </summary>
    Task CancelAsync(long id, long userId);

    /// <summary>
    /// 添加参与者
    /// </summary>
    Task AddAttendeesAsync(long eventId, List<long> userIds, bool isRequired = true);

    /// <summary>
    /// 移除参与者
    /// </summary>
    Task RemoveAttendeeAsync(long eventId, long userId);

    /// <summary>
    /// 回复邀请
    /// </summary>
    Task RespondAsync(long eventId, long userId, int responseStatus);
}
