using STOTOP.Core.Models;

namespace STOTOP.Module.Finance.Entities;

public class FinAccount : BaseEntity, IOrgScoped
{
    public string FCode { get; set; } = string.Empty;
    public string FName { get; set; } = string.Empty;
    public string FCategory { get; set; } = string.Empty;
    public string FBalanceDirection { get; set; } = string.Empty;
    public int FLevel { get; set; }
    public long FParentId { get; set; }
    public int FIsLeaf { get; set; }
    public string? FAuxiliary { get; set; }
    public string? FCurrency { get; set; }
    public string? FUnit { get; set; }
    public int FEnableStatus { get; set; }
    public long FAccountSetId { get; set; }
    public long FOrgId { get; set; }  // 组织ID
    /// <summary>启用年度</summary>
    public int F启用年度 { get; set; }
    /// <summary>启用期间</summary>
    public int F启用期间 { get; set; }
    public DateTime FCreatedTime { get; set; }
    public DateTime FUpdatedTime { get; set; }
}
