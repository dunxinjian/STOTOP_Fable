using STOTOP.Core.Models;

namespace STOTOP.Module.Vehicle.Entities;

public class VehInsurance : BaseEntity, IOrgScoped
{
    public string FUID { get; set; } = Guid.NewGuid().ToString("N");
    public long F车辆ID { get; set; }
    public string F保险类型 { get; set; } = string.Empty;
    public string? F保险公司 { get; set; }
    public string? F保单号 { get; set; }
    public decimal? F保费 { get; set; }
    public DateTime F生效日期 { get; set; }
    public DateTime F到期日期 { get; set; }
    public int F保险状态 { get; set; }
    public string? F备注 { get; set; }
    public long? F创建人ID { get; set; }
    public DateTime F创建时间 { get; set; }
    public DateTime F更新时间 { get; set; }
    public long FOrgId { get; set; }
}
