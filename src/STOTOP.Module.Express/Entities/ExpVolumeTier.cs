using STOTOP.Core.Models;

namespace STOTOP.Module.Express.Entities;

public class ExpVolumeTier : BaseEntity, IOrgScoped
{
    public long F业务对象ID { get; set; }
    public int F最低月发件量 { get; set; }
    public long F报价方案ID { get; set; }
    public bool F启用 { get; set; } = true;
    public long FOrgId { get; set; }
    public string F品牌编码 { get; set; } = string.Empty;
}
