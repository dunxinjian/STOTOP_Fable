using STOTOP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace STOTOP.Module.CardFlow.Services.Import.Parsers;

public class SourceParserFactory
{
    private readonly STOTOPDbContext _context;
    private readonly DynamicSourceParser _dynamicParser;

    public SourceParserFactory(
        STOTOPDbContext context,
        DynamicSourceParser dynamicParser)
    {
        _context = context;
        _dynamicParser = dynamicParser;
    }

    /// <summary>根据目标表名获取 Parser（DC文件类型已废除，用于向后兼容）</summary>
    public Task<ISourceParser> GetParserAsync(string targetTable)
    {
        // DC文件类型表已废除，该方法不再支持从数据库加载配置
        throw new InvalidOperationException($"不支持的目标表: {targetTable}，请使用管道 AutoPlugin 配置解析规则");
    }
}
