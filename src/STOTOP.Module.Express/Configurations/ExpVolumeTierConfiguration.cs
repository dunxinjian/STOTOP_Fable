using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.Express.Entities;

namespace STOTOP.Module.Express.Configurations;

public class ExpVolumeTierConfiguration : IEntityTypeConfiguration<ExpVolumeTier>
{
    public void Configure(EntityTypeBuilder<ExpVolumeTier> builder)
    {
        builder.ToTable("EXP发件量阶梯");

        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.F业务对象ID).HasColumnName("F业务对象ID");
        builder.Property(e => e.F最低月发件量).HasColumnName("F最低月发件量");
        builder.Property(e => e.F报价方案ID).HasColumnName("F报价方案ID");
        builder.Property(e => e.F启用).HasColumnName("F启用").HasDefaultValue(true);
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID").HasDefaultValue(0);
        builder.Property(e => e.F品牌编码).HasColumnName("F品牌编码").HasColumnType("nchar(2)");

        builder.HasIndex(e => new { e.F业务对象ID, e.F品牌编码, e.F最低月发件量 })
            .HasDatabaseName("IX_EXP发件量阶梯_业务对象品牌发件量");
    }
}
