using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaCalendarEventConfiguration : IEntityTypeConfiguration<OaCalendarEvent>
{
    public void Configure(EntityTypeBuilder<OaCalendarEvent> builder)
    {
        builder.ToTable("OA日程事件");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FTitle).HasColumnName("F标题").HasMaxLength(200);
        builder.Property(e => e.FDescription).HasColumnName("F描述").HasMaxLength(2000);
        builder.Property(e => e.FLocation).HasColumnName("F地点").HasMaxLength(200);
        builder.Property(e => e.FStartTime).HasColumnName("F开始时间");
        builder.Property(e => e.FEndTime).HasColumnName("F结束时间");
        builder.Property(e => e.FActualStartTime).HasColumnName("F实际开始时间");
        builder.Property(e => e.FActualEndTime).HasColumnName("F实际结束时间");
        builder.Property(e => e.FStatus).HasColumnName("F状态");
        builder.Property(e => e.FPriority).HasColumnName("F优先级");
        builder.Property(e => e.FIsAllDay).HasColumnName("F全天事件");
        builder.Property(e => e.FIsRecurring).HasColumnName("F是否周期");
        builder.Property(e => e.FRecurrenceRule).HasColumnName("F周期规则").HasMaxLength(500);
        builder.Property(e => e.FRecurrenceEndDate).HasColumnName("F周期结束日期");
        builder.Property(e => e.FParentEventId).HasColumnName("F父事件ID");
        builder.Property(e => e.FOrganizerId).HasColumnName("F组织者ID");
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID");
        builder.Property(e => e.FDingTalkEventId).HasColumnName("F钉钉事件ID").HasMaxLength(200);
        builder.Property(e => e.FDingTalkCalendarId).HasColumnName("F钉钉日历ID").HasMaxLength(200);
        builder.Property(e => e.FSyncStatus).HasColumnName("F同步状态");
        builder.Property(e => e.FLastSyncTime).HasColumnName("F最后同步时间");
        builder.Property(e => e.FColor).HasColumnName("F颜色标记").HasMaxLength(20);
        builder.Property(e => e.FRemindMinutes).HasColumnName("F提醒分钟");
        builder.Property(e => e.FCreateTime).HasColumnName("F创建时间");
        builder.Property(e => e.FUpdateTime).HasColumnName("F更新时间");

        // 索引
        builder.HasIndex(e => new { e.FOrgId, e.FStartTime, e.FEndTime }).HasDatabaseName("IX_OA日程事件_组织时间");
        builder.HasIndex(e => e.FStatus).HasDatabaseName("IX_OA日程事件_状态");
        builder.HasIndex(e => e.FOrganizerId).HasDatabaseName("IX_OA日程事件_组织者");
        builder.HasIndex(e => e.FDingTalkEventId)
            .IsUnique()
            .HasFilter("[F钉钉事件ID] IS NOT NULL")
            .HasDatabaseName("IX_OA日程事件_钉钉事件ID");
        builder.HasIndex(e => e.FParentEventId).HasDatabaseName("IX_OA日程事件_父事件");

        // 自引用关系
        builder.HasOne(e => e.ParentEvent)
            .WithMany(e => e.ChildEvents)
            .HasForeignKey(e => e.FParentEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
