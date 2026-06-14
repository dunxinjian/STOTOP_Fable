using STOTOP.Core.Models;

namespace STOTOP.Module.Finance.Entities;

public class FinAmoebaAllocation : BaseEntity, IOrgScoped
{
    public long FUnitId { get; set; }           // F经营单元ID
    public string FBrandCode { get; set; } = string.Empty; // F品牌编码
    public int FAllocationType { get; set; }    // F分摊方式: 1=固定比例 2=动态计算
    public decimal? FOutboundRatio { get; set; } // F出港比例
    public decimal? FInboundRatio { get; set; }  // F进港比例
    public long FOrgId { get; set; }            // F组织ID
}
