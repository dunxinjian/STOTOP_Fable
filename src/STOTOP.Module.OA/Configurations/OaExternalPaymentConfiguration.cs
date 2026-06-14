using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaExternalPaymentConfiguration : IEntityTypeConfiguration<OaExternalPayment>
{
    public void Configure(EntityTypeBuilder<OaExternalPayment> builder)
    {
        builder.ToTable("OA对外付款单");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FDocNumber).HasColumnName("F单据编号").HasMaxLength(30);
        builder.Property(e => e.FApplicantId).HasColumnName("F申请人ID");
        builder.Property(e => e.FDeptId).HasColumnName("F部门ID");
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID");
        builder.Property(e => e.FPaymentReason).HasColumnName("F付款事由").HasMaxLength(500);
        builder.Property(e => e.FTotalAmount).HasColumnName("F付款总金额").HasColumnType("decimal(18,2)");
        builder.Property(e => e.FRequestRefId).HasColumnName("F引用请款单ID");
        builder.Property(e => e.FPayeeName).HasColumnName("F收款方名称").HasMaxLength(200);
        builder.Property(e => e.FPayeeAccount).HasColumnName("F收款方账号").HasMaxLength(50);
        builder.Property(e => e.FPayeeBank).HasColumnName("F收款方开户行").HasMaxLength(200);
        builder.Property(e => e.FPaymentMethod).HasColumnName("F付款方式").HasMaxLength(20);
        builder.Property(e => e.FExpectedPayDate).HasColumnName("F期望付款日期");
        builder.Property(e => e.FContractNo).HasColumnName("F合同编号").HasMaxLength(50);
        builder.Property(e => e.FInvoiceNo).HasColumnName("F发票号").HasMaxLength(50);
        builder.Property(e => e.FAttachmentCount).HasColumnName("F附件数");
        builder.Property(e => e.FRemark).HasColumnName("F备注").HasMaxLength(500);
        builder.Property(e => e.FDocStatus).HasColumnName("F单据状态");
        builder.Property(e => e.FVoucherId).HasColumnName("F凭证ID");
        builder.Property(e => e.FCreatedTime).HasColumnName("F创建时间");
        builder.Property(e => e.FModifiedTime).HasColumnName("F修改时间");

        builder.HasIndex(e => e.FDocNumber).IsUnique().HasDatabaseName("IX_OA对外付款单_编号");
        builder.HasIndex(e => new { e.FOrgId, e.FDocStatus }).HasDatabaseName("IX_OA对外付款单_组织状态");

        builder.HasMany(e => e.Details)
            .WithOne()
            .HasForeignKey(d => d.FPaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
