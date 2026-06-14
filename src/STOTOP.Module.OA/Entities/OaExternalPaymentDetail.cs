using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaExternalPaymentDetail : BaseEntity
{
    public long FPaymentId { get; set; }
    public int FLineNo { get; set; }
    public string FExpenseType { get; set; } = string.Empty;
    public string? FExpenseAccountCode { get; set; }
    public string FSummary { get; set; } = string.Empty;
    public decimal FAmount { get; set; }
    public string? FRemark { get; set; }
}
