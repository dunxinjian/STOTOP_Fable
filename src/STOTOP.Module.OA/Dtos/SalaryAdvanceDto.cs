namespace STOTOP.Module.OA.Dtos;

public class SalaryAdvanceDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public long ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public long DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public decimal AdvanceAmount { get; set; }
    public string AdvanceMonth { get; set; } = string.Empty;
    public string ApplyReason { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime? ModifiedTime { get; set; }
}

public class CreateSalaryAdvanceRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public decimal AdvanceAmount { get; set; }
    public string AdvanceMonth { get; set; } = string.Empty;
    public string ApplyReason { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
}

public class UpdateSalaryAdvanceRequest
{
    public decimal AdvanceAmount { get; set; }
    public string AdvanceMonth { get; set; } = string.Empty;
    public string ApplyReason { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
}
