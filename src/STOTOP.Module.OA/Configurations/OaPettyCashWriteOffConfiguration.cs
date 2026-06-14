using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaPettyCashWriteOffConfiguration : IEntityTypeConfiguration<OaPettyCashWriteOff>
{
    public void Configure(EntityTypeBuilder<OaPettyCashWriteOff> builder)
    {
        builder.ToTable("OA备用金冲销单");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FDocNumber).HasColumnName("F单据编号").HasMaxLength(30);
        builder.Property(e => e.FApplicantId).HasColumnName("F申请人ID");
        builder.Property(e => e.FDeptId).HasColumnName("F部门ID");
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID");
        builder.Property(e => e.FApplicationRefId).HasColumnName("F关联申请单ID");
        builder.Property(e => e.FOriginalAmount).HasColumnName("F备用金原额").HasColumnType("decimal(18,2)");
        builder.Property(e => e.FReimbursedTotal).HasColumnName("F报销总额").HasColumnType("decimal(18,2)");
        builder.Property(e => e.FReturnedTotal).HasColumnName("F还款总额").HasColumnType("decimal(18,2)");
        builder.Property(e => e.FDifference).HasColumnName("F差额").HasColumnType("decimal(18,2)");
        builder.Property(e => e.FDifferenceDirection).HasColumnName("F差额方向").HasMaxLength(20);
        builder.Property(e => e.FRemark).HasColumnName("F备注").HasMaxLength(500);
        builder.Property(e => e.FDocStatus).HasColumnName("F单据状态");
        builder.Property(e => e.FVoucherId).HasColumnName("F凭证ID");
        builder.Property(e => e.FCreatedTime).HasColumnName("F创建时间");

        builder.HasIndex(e => e.FDocNumber).IsUnique().HasDatabaseName("IX_OA备用金冲销单_编号");

        builder.HasOne<OaPettyCashApplication>()
            .WithMany()
            .HasForeignKey(e => e.FApplicationRefId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_备用金冲销_申请单");
    }
}
