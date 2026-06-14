using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaExternalPaymentDetailConfiguration : IEntityTypeConfiguration<OaExternalPaymentDetail>
{
    public void Configure(EntityTypeBuilder<OaExternalPaymentDetail> builder)
    {
        builder.ToTable("OA对外付款单_明细");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FPaymentId).HasColumnName("F付款单ID");
        builder.Property(e => e.FLineNo).HasColumnName("F行号");
        builder.Property(e => e.FExpenseType).HasColumnName("F费用类型").HasMaxLength(50);
        builder.Property(e => e.FExpenseAccountCode).HasColumnName("F费用科目编码").HasMaxLength(30);
        builder.Property(e => e.FSummary).HasColumnName("F摘要").HasMaxLength(200);
        builder.Property(e => e.FAmount).HasColumnName("F金额").HasColumnType("decimal(18,2)");
        builder.Property(e => e.FRemark).HasColumnName("F备注").HasMaxLength(200);

        builder.HasIndex(e => e.FPaymentId).HasDatabaseName("IX_OA对外付款单_明细_主表");
    }
}
