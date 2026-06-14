using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaPettyCashApplication : BaseEntity, IOrgScoped
{
    public string FDocNumber { get; set; } = string.Empty;
    public long FApplicantId { get; set; }
    public long FDeptId { get; set; }
    public long FOrgId { get; set; }
    public decimal FApplyAmount { get; set; }
    public string FApplyReason { get; set; } = string.Empty;
    public DateOnly? FExpectedReturnDate { get; set; }
    public string FPaymentMethod { get; set; } = string.Empty;
    public string FPayeeName { get; set; } = string.Empty;
    public string? FPayeeAccount { get; set; }
    public string? FPayeeBank { get; set; }
    public string? FRemark { get; set; }
    public int FDocStatus { get; set; }
    public decimal FReimbursedAmount { get; set; }
    public decimal FRepaidAmount { get; set; }
    public decimal FOutstandingBalance { get; set; }
    public long? FVoucherId { get; set; }
    public DateTime FCreatedTime { get; set; }
}
