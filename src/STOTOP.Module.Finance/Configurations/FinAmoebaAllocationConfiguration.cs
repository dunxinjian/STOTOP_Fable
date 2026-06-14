using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STOTOP.Module.Finance.Entities;

namespace STOTOP.Module.Finance.Configurations;

public class FinAmoebaAllocationConfiguration : IEntityTypeConfiguration<FinAmoebaAllocation>
{
    public void Configure(EntityTypeBuilder<FinAmoebaAllocation> builder)
    {
        builder.ToTable("FIN阿米巴分摊比例");
        
        builder.Property(e => e.FID).HasColumnName("FID");
        builder.Property(e => e.FUnitId).HasColumnName("F经营单元ID");
        builder.Property(e => e.FBrandCode).HasColumnName("F品牌编码").HasMaxLength(2).IsFixedLength();
        builder.Property(e => e.FAllocationType).HasColumnName("F分摊方式");
        builder.Property(e => e.FOutboundRatio).HasColumnName("F出港比例").HasColumnType("decimal(5,2)");
        builder.Property(e => e.FInboundRatio).HasColumnName("F进港比例").HasColumnType("decimal(5,2)");
        builder.Property(e => e.FOrgId).HasColumnName("F组织ID").HasDefaultValue(192L);
        
        builder.HasIndex(e => new { e.FUnitId, e.FBrandCode })
            .IsUnique()
            .HasDatabaseName("IX_FIN阿米巴分摊比例_单元品牌");
    }
}
