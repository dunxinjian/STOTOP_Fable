namespace STOTOP.Module.CardFlow.Services.Import.Parsers;

public interface ISourceParser
{
    /// <summary>此 Parser 写入的目标暂存表名</summary>
    string TargetTable { get; }

    /// <summary>
    /// 解析文件并批量写入对应的暂存表
    /// </summary>
    /// <param name="batchId">批次ID</param>
    /// <param name="filePath">服务器上的文件路径</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>解析结果（成功数、失败数、总行数）</returns>
    Task<ParseResult> ParseAndImportAsync(long batchId, string filePath, CancellationToken ct = default);
}

public class ParseResult
{
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailRows { get; set; }
    public int SkippedRows { get; set; } // 合计行等跳过的行
}
