namespace STOTOP.Module.CardFlow.Services.Import;

public interface IImportStage
{
    string StageName { get; }
    Task<ImportStageResult> ExecuteAsync(ImportContext context);
}

public class ImportStageResult
{
    public bool Success { get; set; }
    public bool IsCritical { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ImportRowError> RowErrors { get; set; } = new();
}

public class ImportRowError
{
    public int RowNumber { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string SeverityLevel { get; set; } = "error";
    public string? ErrorField { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? SuggestedFix { get; set; }
    public string? OriginalValue { get; set; }
    public string? QualityDimension { get; set; }
}

public class ImportContext
{
    public long BatchId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public Stream? FileStream { get; set; }
    public long FileSize { get; set; }
    public int HeaderRow { get; set; } = 1;
    public List<string> ColumnNames { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
    public CancellationToken CancellationToken { get; set; }
    public string? TargetTable { get; set; }
    public string? ColumnMappingJson { get; set; }
    public string? DecimalFieldsJson { get; set; }
    public string? SerialNumberRule { get; set; }
    public string? TotalRowDetectionConfig { get; set; }
    /// <summary>业务主键字段（Excel列名）JSON数组，用于生成 F业务主键 SHA256 哈希</summary>
    public string? KeyFieldsJson { get; set; }

    /// <summary>当前组织ID（用于 STG 表数据隔离）</summary>
    public long OrgId { get; set; }
    /// <summary>数据作用域ID（通常等于批次ID，用于 STG 表数据隔离）</summary>
    public string? DataScopeId { get; set; }
    /// <summary>关联工作项ID（可选）</summary>
    public long? WorkItemId { get; set; }
}
