using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaSalaryAdvanceConfiguration : IEntityTypeConfiguration<OaSalaryAdvance>
{
    public void Configure(EntityTypeBuilder<OaSalaryAdvance> builder)
    {
        builder.ToTable("OA预支工资单");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FDocNumber).HasColumnName("F单据编号").HasMaxLength(30);
        builder.Property(e => e.FApplicantId).HasColumnName("F申请人PID");
        builder.Property(e => e.FDeptId).HasColumnName("F部门ID");
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID");
        builder.Property(e => e.FAdvanceAmount).HasColumnName("F预支金额").HasColumnType("decimal(18,2)");
        builder.Property(e => e.FAdvanceMonth).HasColumnName("F预支月份").HasMaxLength(10);
        builder.Property(e => e.FApplyReason).HasColumnName("F申请事由").HasMaxLength(500);
        builder.Property(e => e.FPaymentMethod).HasColumnName("F收款方式").HasMaxLength(20);
        builder.Property(e => e.FPayeeName).HasColumnName("F收款人名称").HasMaxLength(200);
        builder.Property(e => e.FPayeeAccount).HasColumnName("F收款人账号").HasMaxLength(50);
        builder.Property(e => e.FPayeeBank).HasColumnName("F收款人开户行").HasMaxLength(200);
        builder.Property(e => e.FRemark).HasColumnName("F备注").HasMaxLength(500);
        builder.Property(e => e.FDocStatus).HasColumnName("F单据状态");
        builder.Property(e => e.FVoucherId).HasColumnName("F凭证ID");
        builder.Property(e => e.FCreatedTime).HasColumnName("F创建时间");
        builder.Property(e => e.FModifiedTime).HasColumnName("F修改时间");

        builder.HasIndex(e => e.FDocNumber).IsUnique().HasDatabaseName("IX_OA预支工资单_编号");
        builder.HasIndex(e => new { e.FOrgId, e.FDocStatus }).HasDatabaseName("IX_OA预支工资单_组织状态");
    }
}
