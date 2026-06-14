using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaExpenseAccountMapping : BaseEntity
{
    public long FExpenseTypeId { get; set; }
    public long FAccountId { get; set; }
    public string FAccountCode { get; set; } = string.Empty;
    public string FAccountName { get; set; } = string.Empty;
    public long FOrgId { get; set; }
    public int FIsDefault { get; set; }
}
