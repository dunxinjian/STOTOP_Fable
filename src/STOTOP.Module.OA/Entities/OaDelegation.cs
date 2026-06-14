using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaDelegation : BaseEntity, IOrgScoped
{
    public long FDelegatorId { get; set; }
    public long FDelegateeId { get; set; }
    public long FOrgId { get; set; }
    public string? FProcessType { get; set; }
    public DateOnly FStartDate { get; set; }
    public DateOnly FEndDate { get; set; }
    public string? FReason { get; set; }
    public int FStatus { get; set; }
    public DateTime FCreatedTime { get; set; }
}
