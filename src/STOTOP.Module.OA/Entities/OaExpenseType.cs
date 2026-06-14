using STOTOP.Core.Models;

namespace STOTOP.Module.OA.Entities;

public class OaExpenseType : BaseEntity, IOrgScoped
{
    public string FTypeCode { get; set; } = string.Empty;
    public string FTypeName { get; set; } = string.Empty;
    public string FApplicableScene { get; set; } = string.Empty;
    public long FOrgId { get; set; }
    public int FSortOrder { get; set; }
    public int FIsEnabled { get; set; }
    public DateTime FCreatedTime { get; set; }
}
