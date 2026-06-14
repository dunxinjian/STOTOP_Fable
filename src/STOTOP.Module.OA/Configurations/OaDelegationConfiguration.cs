using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaDelegationConfiguration : IEntityTypeConfiguration<OaDelegation>
{
    public void Configure(EntityTypeBuilder<OaDelegation> builder)
    {
        builder.ToTable("OA审批委托");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FDelegatorId).HasColumnName("F委托人ID");
        builder.Property(e => e.FDelegateeId).HasColumnName("F受托人ID");
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID");
        builder.Property(e => e.FProcessType).HasColumnName("F流程类型").HasMaxLength(50);
        builder.Property(e => e.FStartDate).HasColumnName("F生效日期");
        builder.Property(e => e.FEndDate).HasColumnName("F失效日期");
        builder.Property(e => e.FReason).HasColumnName("F委托原因").HasMaxLength(200);
        builder.Property(e => e.FStatus).HasColumnName("F状态");
        builder.Property(e => e.FCreatedTime).HasColumnName("F创建时间");

        builder.HasIndex(e => new { e.FDelegatorId, e.FStatus }).HasDatabaseName("IX_OA审批委托_委托人");
        builder.HasIndex(e => new { e.FDelegateeId, e.FStatus }).HasDatabaseName("IX_OA审批委托_受托人");
    }
}
