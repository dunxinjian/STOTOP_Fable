namespace STOTOP.Module.OA.Dtos;

public class ExternalPaymentDetailDto
{
    public long Id { get; set; }
    public int LineNo { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public string? ExpenseAccountCode { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Remark { get; set; }
}

public class ExternalPaymentDto
{
    public long Id { get; set; }
    public string DocNumber { get; set; } = string.Empty;
    public long ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public long DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public string PaymentReason { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public long? RequestRefId { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public string PayeeBank { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateOnly? ExpectedPayDate { get; set; }
    public string? ContractNo { get; set; }
    public string? InvoiceNo { get; set; }
    public int AttachmentCount { get; set; }
    public string? Remark { get; set; }
    public int DocStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime? ModifiedTime { get; set; }
    public List<ExternalPaymentDetailDto> Details { get; set; } = new();
}

public class CreateExternalPaymentDetailRequest
{
    public string ExpenseType { get; set; } = string.Empty;
    public string? ExpenseAccountCode { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Remark { get; set; }
}

public class CreateExternalPaymentRequest
{
    public long OrgId { get; set; }
    public long DeptId { get; set; }
    public string PaymentReason { get; set; } = string.Empty;
    public long? RequestRefId { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public string PayeeBank { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateOnly? ExpectedPayDate { get; set; }
    public string? ContractNo { get; set; }
    public string? InvoiceNo { get; set; }
    public string? Remark { get; set; }
    public List<CreateExternalPaymentDetailRequest> Details { get; set; } = new();
}

public class UpdateExternalPaymentRequest
{
    public string PaymentReason { get; set; } = string.Empty;
    public long? RequestRefId { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public string PayeeBank { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateOnly? ExpectedPayDate { get; set; }
    public string? ContractNo { get; set; }
    public string? InvoiceNo { get; set; }
    public string? Remark { get; set; }
    public List<CreateExternalPaymentDetailRequest> Details { get; set; } = new();
}
