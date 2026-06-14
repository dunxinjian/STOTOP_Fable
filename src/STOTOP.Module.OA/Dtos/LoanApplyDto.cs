namespace STOTOP.Module.OA.Dtos;

public class LoanApplyDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public long ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public long DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public string LoanReason { get; set; } = string.Empty;
    public DateOnly? ExpectedReturnDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public decimal ReimburseOffsetAmount { get; set; }
    public decimal RepaidAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? ModifiedTime { get; set; }
}

public class CreateLoanApplyRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public decimal LoanAmount { get; set; }
    public string LoanReason { get; set; } = string.Empty;
    public DateOnly? ExpectedReturnDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
}

public class UpdateLoanApplyRequest
{
    public decimal LoanAmount { get; set; }
    public string LoanReason { get; set; } = string.Empty;
    public DateOnly? ExpectedReturnDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
}

public class LoanLedgerDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public decimal ReimburseOffsetAmount { get; set; }
    public decimal RepaidAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
}
