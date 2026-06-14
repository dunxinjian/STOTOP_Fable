namespace STOTOP.Module.OA.Dtos;

public class ExpenseReimburseDetailDto
{
    public long Id { get; set; }
    public int LineNo { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public string? ExpenseAccountCode { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly OccurDate { get; set; }
    public string? Remark { get; set; }
}

public class ExpenseReimburseDto
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
    public decimal TotalAmount { get; set; }
    public long? RequestRefId { get; set; }
    public long? LoanRefId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public string? PayeeBank { get; set; }
    public int AttachmentCount { get; set; }
    public string? Remark { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime? ModifiedTime { get; set; }
    public List<ExpenseReimburseDetailDto> Details { get; set; } = new();
}

public class CreateExpenseReimburseDetailRequest
{
    public string ExpenseType { get; set; } = string.Empty;
    public string? ExpenseAccountCode { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly OccurDate { get; set; }
    public string? Remark { get; set; }
}

public class CreateExpenseReimburseRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long? RequestRefId { get; set; }
    public long? LoanRefId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
    public List<CreateExpenseReimburseDetailRequest> Details { get; set; } = new();
}

public class UpdateExpenseReimburseRequest
{
    public string Reason { get; set; } = string.Empty;
    public long? RequestRefId { get; set; }
    public long? LoanRefId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
    public List<CreateExpenseReimburseDetailRequest> Details { get; set; } = new();
}

public class AvailableRequestDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ReferencedAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
}

public class AvailableLoanDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public string LoanReason { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
}
