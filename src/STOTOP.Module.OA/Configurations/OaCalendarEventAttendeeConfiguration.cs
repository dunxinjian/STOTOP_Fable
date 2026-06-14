using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaCalendarEventAttendeeConfiguration : IEntityTypeConfiguration<OaCalendarEventAttendee>
{
    public void Configure(EntityTypeBuilder<OaCalendarEventAttendee> builder)
    {
        builder.ToTable("OA日程参与者");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FEventId).HasColumnName("F事件ID");
        builder.Property(e => e.FUserId).HasColumnName("F用户ID");
        builder.Property(e => e.FResponseStatus).HasColumnName("F响应状态");
        builder.Property(e => e.FAttendStatus).HasColumnName("F出席状态");
        builder.Property(e => e.FIsRequired).HasColumnName("F是否必须");
        builder.Property(e => e.FCreateTime).HasColumnName("F创建时间");

        // 索引
        builder.HasIndex(e => e.FEventId).HasDatabaseName("IX_OA日程参与者_事件");
        builder.HasIndex(e => new { e.FUserId, e.FEventId })
            .IsUnique()
            .HasDatabaseName("IX_OA日程参与者_用户事件");

        // 外键关系
        builder.HasOne(e => e.Event)
            .WithMany(e => e.Attendees)
            .HasForeignKey(e => e.FEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
