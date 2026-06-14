namespace STOTOP.Module.OA.Dtos;

public class ExpenseAccountMappingDto
{
    public long Id { get; set; }
    public long ExpenseTypeId { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public bool IsDefault { get; set; }
}

public class CreateExpenseAccountMappingRequest
{
    public long ExpenseTypeId { get; set; }
    public long AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdateExpenseAccountMappingRequest
{
    public long AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
