namespace STOTOP.Module.OA.Dtos;

public class ExpenseTypeDto
{
    public long Id { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string ApplicableScene { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class CreateExpenseTypeRequest
{
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string ApplicableScene { get; set; } = string.Empty;
    public long OrgId { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateExpenseTypeRequest
{
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string ApplicableScene { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
