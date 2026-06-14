namespace STOTOP.Module.OA.Dtos;

public class ExpenseRequestDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public long ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public long DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? ExpectedPayDate { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public string? PayeeName { get; set; }
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public decimal ReferencedAmount { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? ModifiedTime { get; set; }
}

public class CreateExpenseRequestRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public DateOnly? ExpectedPayDate { get; set; }
    public string? PayeeName { get; set; }
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
}

public class UpdateExpenseRequestRequest
{
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public DateOnly? ExpectedPayDate { get; set; }
    public string? PayeeName { get; set; }
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
}
