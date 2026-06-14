using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaSalaryAdvance : BaseEntity, IOrgScoped
{
    public string FDocNumber { get; set; } = string.Empty;
    public long FApplicantId { get; set; }
    public long FDeptId { get; set; }
    public long FOrgId { get; set; }
    public decimal FAdvanceAmount { get; set; }
    public string FAdvanceMonth { get; set; } = string.Empty;
    public string FApplyReason { get; set; } = string.Empty;
    public string FPaymentMethod { get; set; } = string.Empty;
    public string FPayeeName { get; set; } = string.Empty;
    public string? FPayeeAccount { get; set; }
    public string? FPayeeBank { get; set; }
    public string? FRemark { get; set; }
    public int FDocStatus { get; set; }
    public long? FVoucherId { get; set; }
    public DateTime FCreatedTime { get; set; }
    public DateTime? FModifiedTime { get; set; }
}
