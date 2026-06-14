using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaExternalPayment : BaseEntity
{
    public string FDocNumber { get; set; } = string.Empty;
    public long FApplicantId { get; set; }
    public long FDeptId { get; set; }
    public long FOrgId { get; set; }
    public string FPaymentReason { get; set; } = string.Empty;
    public decimal FTotalAmount { get; set; }
    public long? FRequestRefId { get; set; }
    public string FPayeeName { get; set; } = string.Empty;
    public string FPayeeAccount { get; set; } = string.Empty;
    public string FPayeeBank { get; set; } = string.Empty;
    public string FPaymentMethod { get; set; } = string.Empty;
    public DateOnly? FExpectedPayDate { get; set; }
    public string? FContractNo { get; set; }
    public string? FInvoiceNo { get; set; }
    public int FAttachmentCount { get; set; }
    public string? FRemark { get; set; }
    public int FDocStatus { get; set; }
    public long? FVoucherId { get; set; }
    public DateTime FCreatedTime { get; set; }
    public DateTime? FModifiedTime { get; set; }

    public virtual ICollection<OaExternalPaymentDetail> Details { get; set; } = new List<OaExternalPaymentDetail>();
}
