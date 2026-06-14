namespace STOTOP.Module.OA.Dtos;

public class DelegationDto
{
    public long Id { get; set; }
    public long DelegatorId { get; set; }
    public string DelegatorName { get; set; } = string.Empty;
    public long DelegateeId { get; set; }
    public string DelegateeName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public string? ProcessType { get; set; }
    public string? ProcessTypeName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }
    public int Status { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class CreateDelegationRequest
{
    public long DelegateeId { get; set; }
    public long OrgId { get; set; }
    public string? ProcessType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }
}
