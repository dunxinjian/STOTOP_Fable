namespace STOTOP.Module.OA.Dtos;

// === 备用金申请 ===
public class PettyCashApplyDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public long ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public long DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public decimal ApplyAmount { get; set; }
    public string ApplyReason { get; set; } = string.Empty;
    public DateOnly? ExpectedReturnDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public decimal ReimbursedAmount { get; set; }
    public decimal RepaidAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class CreatePettyCashApplyRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public decimal ApplyAmount { get; set; }
    public string ApplyReason { get; set; } = string.Empty;
    public DateOnly? ExpectedReturnDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
}

public class UpdatePettyCashApplyRequest
{
    public decimal ApplyAmount { get; set; }
    public string ApplyReason { get; set; } = string.Empty;
    public DateOnly? ExpectedReturnDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string? PayeeAccount { get; set; }
    public string? PayeeBank { get; set; }
    public string? Remark { get; set; }
}

// === 备用金报销 ===
public class PettyCashReimburseDetailDto
{
    public long Id { get; set; }
    public int LineNo { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public string? ExpenseAccountCode { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly OccurDate { get; set; }
}

public class PettyCashReimburseDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public long ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public long ApplicationRefId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int AttachmentCount { get; set; }
    public string? Remark { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public List<PettyCashReimburseDetailDto> Details { get; set; } = new();
}

public class CreatePettyCashReimburseDetailRequest
{
    public string ExpenseType { get; set; } = string.Empty;
    public string? ExpenseAccountCode { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly OccurDate { get; set; }
}

public class CreatePettyCashReimburseRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public long ApplicationRefId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public List<CreatePettyCashReimburseDetailRequest> Details { get; set; } = new();
}

// === 备用金还款 ===
public class PettyCashReturnDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public long ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public long ApplicationRefId { get; set; }
    public decimal ReturnAmount { get; set; }
    public string ReturnMethod { get; set; } = string.Empty;
    public string? ReturnNote { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
}

public class CreatePettyCashReturnRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public long ApplicationRefId { get; set; }
    public decimal ReturnAmount { get; set; }
    public string ReturnMethod { get; set; } = string.Empty;
    public string? ReturnNote { get; set; }
}

// === 备用金冲销 ===
public class PettyCashWriteOffDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public long ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public long ApplicationRefId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal ReimbursedTotal { get; set; }
    public decimal ReturnedTotal { get; set; }
    public decimal Difference { get; set; }
    public string DifferenceDirection { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
}

public class CreatePettyCashWriteOffRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public long ApplicationRefId { get; set; }
    public string? Remark { get; set; }
}

// === 备用金台账 ===
public class PettyCashLedgerDto
{
    public long ApplicationId { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public decimal ApplyAmount { get; set; }
    public decimal ReimbursedAmount { get; set; }
    public decimal RepaidAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
}
