using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaPettyCashWriteOff : BaseEntity
{
    public string FDocNumber { get; set; } = string.Empty;
    public long FApplicantId { get; set; }
    public long FDeptId { get; set; }
    public long FOrgId { get; set; }
    public long FApplicationRefId { get; set; }
    public decimal FOriginalAmount { get; set; }
    public decimal FReimbursedTotal { get; set; }
    public decimal FReturnedTotal { get; set; }
    public decimal FDifference { get; set; }
    public string FDifferenceDirection { get; set; } = string.Empty;
    public string? FRemark { get; set; }
    public int FDocStatus { get; set; }
    public long? FVoucherId { get; set; }
    public DateTime FCreatedTime { get; set; }
}
