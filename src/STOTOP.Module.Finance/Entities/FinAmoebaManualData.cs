using STOTOP.Core.Models;

namespace STOTOP.Module.Finance.Entities;

public class FinAmoebaManualData : BaseEntity
{
    public long FTemplateId { get; set; }
    public long? FPLItemId { get; set; }       // 关联损益项（暂估数据可为空）
    public long FOrgId { get; set; }           // 组织
    public string FPeriod { get; set; } = ""; // 期间标签 "202603"
    public decimal FAmount { get; set; }       // 金额/主数值
    public decimal? FPerUnitValue { get; set; } // 单票&均值（手工填报时使用）
    public string FDataType { get; set; } = "manual"; // 数据类型：manual / estimate
    public string? FAccountCode { get; set; }   // 暂估关联科目编码（FDataType=estimate时使用）
    public string? FAuxiliaryJson { get; set; } // 暂估辅助核算标签JSON
    public DateTime FCreatedTime { get; set; }
    public DateTime FUpdatedTime { get; set; }
}
