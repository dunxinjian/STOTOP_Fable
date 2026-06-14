using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.OA.Entities;

namespace STOTOP.Module.OA.Configurations;

public class OaExpenseAccountMappingConfiguration : IEntityTypeConfiguration<OaExpenseAccountMapping>
{
    public void Configure(EntityTypeBuilder<OaExpenseAccountMapping> builder)
    {
        builder.ToTable("OA费用类型科目映射");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FExpenseTypeId).HasColumnName("F费用类型ID");
        builder.Property(e => e.FAccountId).HasColumnName("F科目ID");
        builder.Property(e => e.FAccountCode).HasColumnName("F科目编码").HasMaxLength(30);
        builder.Property(e => e.FAccountName).HasColumnName("F科目名称").HasMaxLength(100);
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID");
        builder.Property(e => e.FIsDefault).HasColumnName("F是否默认");

        builder.HasIndex(e => new { e.FOrgId, e.FExpenseTypeId }).HasDatabaseName("IX_OA费用类型科目映射_组织");

        builder.HasOne<OaExpenseType>()
            .WithMany()
            .HasForeignKey(e => e.FExpenseTypeId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_类型科目_类型");
    }
}
