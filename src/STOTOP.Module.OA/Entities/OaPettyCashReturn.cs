using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaPettyCashReturn : BaseEntity, IOrgScoped
{
    public string FDocNumber { get; set; } = string.Empty;
    public long FApplicantId { get; set; }
    public long FDeptId { get; set; }
    public long FOrgId { get; set; }
    public long FApplicationRefId { get; set; }
    public decimal FReturnAmount { get; set; }
    public string FReturnMethod { get; set; } = string.Empty;
    public string? FReturnNote { get; set; }
    public int FDocStatus { get; set; }
    public long? FVoucherId { get; set; }
    public DateTime FCreatedTime { get; set; }
}
