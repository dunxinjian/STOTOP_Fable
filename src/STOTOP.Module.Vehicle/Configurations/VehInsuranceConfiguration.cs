using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.Vehicle.Entities;

namespace STOTOP.Module.Vehicle.Configurations;

public class VehInsuranceConfiguration : IEntityTypeConfiguration<VehInsurance>
{
    public void Configure(EntityTypeBuilder<VehInsurance> builder)
    {
        builder.ToTable("VEH保险记录");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FUID).HasColumnName("FUID").HasMaxLength(50);
        builder.Property(e => e.F车辆ID).HasColumnName("F车辆ID");
        builder.Property(e => e.F保险类型).HasColumnName("F保险类型").HasMaxLength(50);
        builder.Property(e => e.F保险公司).HasColumnName("F保险公司").HasMaxLength(200);
        builder.Property(e => e.F保单号).HasColumnName("F保单号").HasMaxLength(100);
        builder.Property(e => e.F保费).HasColumnName("F保费").HasColumnType("decimal(18,2)");
        builder.Property(e => e.F生效日期).HasColumnName("F生效日期");
        builder.Property(e => e.F到期日期).HasColumnName("F到期日期");
        builder.Property(e => e.F保险状态).HasColumnName("F保险状态").HasDefaultValue(1);
        builder.Property(e => e.F备注).HasColumnName("F备注").HasMaxLength(1000);
        builder.Property(e => e.F创建人ID).HasColumnName("F创建人ID");
        builder.Property(e => e.F创建时间).HasColumnName("F创建时间");
        builder.Property(e => e.F更新时间).HasColumnName("F更新时间");
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID").HasDefaultValue(0);

        builder.HasIndex(e => e.F保单号).HasDatabaseName("IX_VEH保险记录_保单号");
        builder.HasIndex(e => e.F车辆ID).HasDatabaseName("IX_VEH保险记录_车辆ID");
        builder.HasIndex(e => e.F到期日期).HasDatabaseName("IX_VEH保险记录_到期日期");
    }
}
