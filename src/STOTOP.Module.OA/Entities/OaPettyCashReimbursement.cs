using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaPettyCashReimbursement : BaseEntity, IOrgScoped
{
    public string FDocNumber { get; set; } = string.Empty;
    public long FApplicantId { get; set; }
    public long FDeptId { get; set; }
    public long FOrgId { get; set; }
    public long FApplicationRefId { get; set; }
    public decimal FTotalAmount { get; set; }
    public string FReason { get; set; } = string.Empty;
    public int FAttachmentCount { get; set; }
    public string? FRemark { get; set; }
    public int FDocStatus { get; set; }
    public long? FVoucherId { get; set; }
    public DateTime FCreatedTime { get; set; }

    public virtual ICollection<OaPettyCashReimbursementDetail> Details { get; set; } = new List<OaPettyCashReimbursementDetail>();
}
