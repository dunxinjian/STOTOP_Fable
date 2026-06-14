using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaExpenseTypeConfiguration : IEntityTypeConfiguration<OaExpenseType>
{
    public void Configure(EntityTypeBuilder<OaExpenseType> builder)
    {
        builder.ToTable("OA费用类型");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FTypeCode).HasColumnName("F类型编码").HasMaxLength(30);
        builder.Property(e => e.FTypeName).HasColumnName("F类型名称").HasMaxLength(50);
        builder.Property(e => e.FApplicableScene).HasColumnName("F适用场景").HasMaxLength(20);
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID");
        builder.Property(e => e.FSortOrder).HasColumnName("F排序");
        builder.Property(e => e.FIsEnabled).HasColumnName("F是否启用");
        builder.Property(e => e.FCreatedTime).HasColumnName("F创建时间");

        builder.HasIndex(e => new { e.FTypeCode, e.FOrgId }).IsUnique().HasDatabaseName("IX_OA费用类型_编码_组织");
    }
}
