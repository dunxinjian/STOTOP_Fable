using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaPettyCashReturnConfiguration : IEntityTypeConfiguration<OaPettyCashReturn>
{
    public void Configure(EntityTypeBuilder<OaPettyCashReturn> builder)
    {
        builder.ToTable("OA备用金还款单");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FDocNumber).HasColumnName("F单据编号").HasMaxLength(30);
        builder.Property(e => e.FApplicantId).HasColumnName("F申请人ID");
        builder.Property(e => e.FDeptId).HasColumnName("F部门ID");
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID");
        builder.Property(e => e.FApplicationRefId).HasColumnName("F关联申请单ID");
        builder.Property(e => e.FReturnAmount).HasColumnName("F还款金额").HasColumnType("decimal(18,2)");
        builder.Property(e => e.FReturnMethod).HasColumnName("F还款方式").HasMaxLength(20);
        builder.Property(e => e.FReturnNote).HasColumnName("F还款说明").HasMaxLength(500);
        builder.Property(e => e.FDocStatus).HasColumnName("F单据状态");
        builder.Property(e => e.FVoucherId).HasColumnName("F凭证ID");
        builder.Property(e => e.FCreatedTime).HasColumnName("F创建时间");

        builder.HasIndex(e => e.FDocNumber).IsUnique().HasDatabaseName("IX_OA备用金还款单_编号");

        builder.HasOne<OaPettyCashApplication>()
            .WithMany()
            .HasForeignKey(e => e.FApplicationRefId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_备用金还款_申请单");
    }
}
