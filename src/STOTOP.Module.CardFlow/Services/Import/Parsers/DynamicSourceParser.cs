using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Services.Import;
using STOTOP.Module.CardFlow.AutoPlugin;
using STOTOP.Module.CardFlow.Services.Import.TransformEngine;

namespace STOTOP.Module.CardFlow.Services.Import.Parsers;

/// <summary>
/// 配置驱动的通用 Parser，从 ParserConfig 的列映射/主键/Decimal字段 读取规则，
/// 支持两种写入模式：
///   1. 指定 FTargetTable 时 → 通过转换引擎写入目标 STG 结构化暂存表
///   2. FTargetTable 为空时 → fallback 写入 STG通用导入数据 表（原有逻辑）
/// </summary>
public class DynamicSourceParser : ISourceParser
{
    private readonly STOTOPDbContext _context;
    private readonly IBulkInsertService _bulkInsertService;
    private readonly IProgressNotifier _progressNotifier;
    private readonly ExcelParserService _excelParserService;
    private readonly ITransformEngine _transformEngine;
    private readonly ILogger<DynamicSourceParser> _logger;

    private const int BulkBatchSize = 5000;
    private const string FallbackTable = "STG通用导入数据";

    private string _targetTable = string.Empty;
    private ParserConfig? _config;

    /// <summary>当前配置的目标暂存表（未 Configure 时返回空字符串）</summary>
    public string TargetTable => _targetTable;
    private List<ColumnMappingItem> _columnMappings = new();
    private List<string> _keyFields = new();
    private List<string> _decimalFields = new();

    // 新增：目标表 + 转换规则
    private List<TransformRule> _transformRules = new();
    private string? _serialNumberRule;
    private TotalRowDetectionConfig? _totalRowConfig;

    public DynamicSourceParser(
        STOTOPDbContext context,
        IBulkInsertService bulkInsertService,
        IProgressNotifier progressNotifier,
        ExcelParserService excelParserService,
        ITransformEngine transformEngine,
        ILogger<DynamicSourceParser> logger)
    {
        _context = context;
        _bulkInsertService = bulkInsertService;
        _progressNotifier = progressNotifier;
        _excelParserService = excelParserService;
        _transformEngine = transformEngine;
        _logger = logger;
    }

    /// <summary>设置当前数据源配置</summary>
    public void Configure(ParserConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // 解析列映射
        _columnMappings = !string.IsNullOrWhiteSpace(config.FColumnMapping)
            ? JsonSerializer.Deserialize<List<ColumnMappingItem>>(config.FColumnMapping, JsonOptions) ?? new()
            : new();

        // 解析业务主键字段列表
        _keyFields = !string.IsNullOrWhiteSpace(config.FKeyFields)
            ? JsonSerializer.Deserialize<List<string>>(config.FKeyFields, JsonOptions) ?? new()
            : new();

        // 解析 Decimal 字段列表
        _decimalFields = !string.IsNullOrWhiteSpace(config.FDecimalFields)
            ? JsonSerializer.Deserialize<List<string>>(config.FDecimalFields, JsonOptions) ?? new()
            : new();

        // 解析目标暂存表（安全校验：必须以 STG 开头）
        _targetTable = string.Empty;
        if (!string.IsNullOrWhiteSpace(config.FTargetTable))
        {
            var table = config.FTargetTable.Trim();
            if (!table.StartsWith("STG", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("目标表 {Table} 不以 STG 开头，将使用通用表 fallback", table);
            }
            else
            {
                StagingTableNameValidator.EnsureSafe(table);
                _targetTable = table;
            }
        }

        // 解析转换规则
        _transformRules = !string.IsNullOrWhiteSpace(config.FTransformRules)
            ? JsonSerializer.Deserialize<List<TransformRule>>(config.FTransformRules, JsonOptions) ?? new()
            : new();

        // 解析流水号规则
        _serialNumberRule = !string.IsNullOrWhiteSpace(config.FSerialNumberRule)
            ? config.FSerialNumberRule.Trim()
            : null;

        // 解析合计行检测规则
        _totalRowConfig = null;
        if (!string.IsNullOrWhiteSpace(config.FTotalRowDetectionRules))
        {
            try { _totalRowConfig = JsonSerializer.Deserialize<TotalRowDetectionConfig>(config.FTotalRowDetectionRules, JsonOptions); }
            catch { _logger.LogWarning("合计行检测规则解析失败，将忽略"); }
        }

        _logger.LogInformation(
            "DynamicSourceParser 已配置: 目标表={TargetTable}, 列映射={MappingCount}, 主键字段={KeyCount}, Decimal字段={DecimalCount}, 转换规则={RuleCount}, 流水号规则={SerialRule}",
            _targetTable, _columnMappings.Count, _keyFields.Count, _decimalFields.Count,
            _transformRules.Count, _serialNumberRule ?? "(无)");
    }

    /// <summary>解析 Excel 文件并写入暂存表</summary>
    public async Task<ParseResult> ParseAndImportAsync(long batchId, string filePath, CancellationToken ct = default)
    {
        if (_config is null)
            throw new InvalidOperationException("请先调用 Configure 方法设置数据源配置");

        var connStr = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("无法获取数据库连接字符串");

        var fileName = Path.GetFileName(filePath);
        // 建立 Excel 列名 → 目标字段的映射
        var excelToDbMap = BuildColumnMap();

        var useTargetTable = !string.IsNullOrEmpty(_targetTable);
        var actualTable = useTargetTable ? _targetTable! : FallbackTable;

        // 构建已映射的 Excel 列名集合（只构建一次）
        var mappedExcelColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in _columnMappings)
        {
            if (!string.IsNullOrEmpty(m.ExcelColumn))
                mappedExcelColumns.Add(m.ExcelColumn);
            if (m.Aliases != null)
            {
                foreach (var alias in m.Aliases)
                {
                    var trimmed = alias.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        mappedExcelColumns.Add(trimmed);
                }
            }
        }

        var result = new ParseResult();
        var errors = new List<CfBatchError>();
        var buffer = new List<Dictionary<string, object?>>();
        int processedRows = 0;

        // 直接打开文件流，避免 ReadAllBytes 触发 MemoryStream 2GB 限制
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await _excelParserService.ParseAsync(stream, fileName, _config.FHeaderRowNumber, _config.FHeaderRowNumber + 1, BulkBatchSize, async (batch, startRow) =>
        {
            foreach (var row in batch)
            {
                ct.ThrowIfCancellationRequested();
                result.TotalRows++;
                processedRows++;

                // 合计行过滤（配置化）
                if (_totalRowConfig?.Enabled == true && IsTotalRow(row, _totalRowConfig))
                {
                    result.SkippedRows++;
                    continue;
                }

                var currentRow = startRow + batch.IndexOf(row);

                try
                {
                    // ① 根据配置进行列映射
                    var mappedData = new Dictionary<string, string>();
                    foreach (var mapping in _columnMappings)
                    {
                        var value = GetValueByMapping(row, mapping);
                        mappedData[mapping.DbColumn] = value;
                    }

                    // ② Decimal 字段解析
                    foreach (var decField in _decimalFields)
                    {
                        if (mappedData.TryGetValue(decField, out var rawVal) && !string.IsNullOrWhiteSpace(rawVal))
                        {
                            if (decimal.TryParse(rawVal, out var decVal))
                            {
                                mappedData[decField] = decVal.ToString();
                            }
                            else
                            {
                                errors.Add(new CfBatchError
                                {
                                    FBatchId = batchId,
                                    FRowNumber = currentRow,
                                    FErrorType = "格式错误",
                                    FSeverityLevel = "Warning",
                                    FErrorField = decField,
                                    FErrorMessage = $"字段 [{decField}] 无法解析为数值",
                                    FOriginalValue = rawVal,
                                    FCreatedTime = DateTime.Now
                                });
                            }
                        }
                    }

                    // ③ 数据转换（如果配置了转换规则）
                    Dictionary<string, object?>? transformedData = null;
                    if (useTargetTable && _transformRules.Count > 0)
                    {
                        transformedData = _transformEngine.Execute(mappedData, _transformRules);
                    }

                    // ③.5 流水号规则应用（占位符中的列名为 Excel 列名，从 row 中取值）
                    if (!string.IsNullOrEmpty(_serialNumberRule))
                    {
                        var serialNumber = Regex.Replace(_serialNumberRule, @"\{(.+?)\}", match =>
                        {
                            var colName = match.Groups[1].Value;
                            // 先尝试从 Excel 原始数据中查找
                            if (row.ContainsKey(colName))
                                return row[colName] ?? "";
                            // 再尝试通过列映射的 dbColumn 反查
                            var mapping = _columnMappings.FirstOrDefault(m => m.DbColumn == colName);
                            if (mapping != null)
                            {
                                if (row.ContainsKey(mapping.ExcelColumn))
                                    return row[mapping.ExcelColumn] ?? "";
                                if (mapping.Aliases != null)
                                {
                                    foreach (var alias in mapping.Aliases)
                                    {
                                        if (row.ContainsKey(alias))
                                            return row[alias] ?? "";
                                    }
                                }
                            }
                            return "";
                        });
                        mappedData["F流水号"] = serialNumber;
                        if (transformedData != null)
                            transformedData["F流水号"] = serialNumber;
                    }

                    // ④ SHA256 业务主键（keyField 为 Excel 列名，从 row 中取值）
                    var keyParts = new StringBuilder();
                    foreach (var keyField in _keyFields)
                    {
                        if (row.TryGetValue(keyField, out var kv))
                            keyParts.Append(kv);
                    }
                    var businessKey = ComputeSha256(keyParts.ToString());

                    // ⑤.5 收集未映射列写入 F其他列数据
                    string? unmappedJson = null;
                    if (useTargetTable)
                    {
                        var unmappedData = new Dictionary<string, string>();
                        foreach (var kv in row)
                        {
                            if (!mappedExcelColumns.Contains(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                            {
                                unmappedData[kv.Key] = kv.Value;
                            }
                        }
                        if (unmappedData.Count > 0)
                        {
                            unmappedJson = JsonSerializer.Serialize(unmappedData, new JsonSerializerOptions { Encoder = global::System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                        }
                    }

                    // ⑥ 构建目标行并写入
                    Dictionary<string, object?> rowDict;
                    if (useTargetTable && transformedData != null)
                    {
                        // 写入结构化 STG 目标表
                        rowDict = BuildTargetTableRow(batchId, currentRow, businessKey, transformedData);
                        if (unmappedJson != null)
                            rowDict["F其他列数据"] = unmappedJson;
                    }
                    else
                    {
                        // fallback: 写入通用表
                        rowDict = BuildFallbackRow(batchId, currentRow, businessKey, mappedData);
                    }

                    buffer.Add(rowDict);
                    result.SuccessRows++;
                }
                catch (Exception ex)
                {
                    result.FailRows++;
                    errors.Add(new CfBatchError
                    {
                        FBatchId = batchId,
                        FRowNumber = currentRow,
                        FErrorType = "解析异常",
                        FSeverityLevel = "Error",
                        FErrorMessage = ex.Message,
                        FCreatedTime = DateTime.Now
                    });
                }
            }

            // 达到批次大小则写入
            if (buffer.Count >= BulkBatchSize)
            {
                await FlushBufferAsync(connStr, actualTable, buffer, ct);
                try { await _progressNotifier.NotifyImportProgressAsync(batchId, processedRows, result.TotalRows, "解析写入暂存"); } catch { }
            }
        }, ct);

        // 写入剩余数据
        if (buffer.Count > 0)
        {
            await FlushBufferAsync(connStr, actualTable, buffer, ct);
        }

        // 持久化批次记录
        {
            var batch = await _context.Set<CfBatch>()
                .AsTracking()
                .FirstOrDefaultAsync(b => b.FID == batchId, ct);
            if (batch != null)
            {
                batch.FActualTargetTable = actualTable;
                await _context.SaveChangesAsync(ct);
            }
        }

        // 写入错误记录
        if (errors.Count > 0)
        {
            await _bulkInsertService.BulkInsertErrorsAsync(errors, ct);
        }

        try { await _progressNotifier.NotifyImportProgressAsync(batchId, processedRows, result.TotalRows, "解析写入暂存"); } catch { }
        _logger.LogInformation("DynamicSourceParser 解析完成：目标表={Table}, 总行数={Total}, 成功={Success}, 失败={Fail}, 跳过={Skip}",
            actualTable, result.TotalRows, result.SuccessRows, result.FailRows, result.SkippedRows);

        return result;
    }

    #region Private Methods

    private static bool IsTotalRow(Dictionary<string, string> row, TotalRowDetectionConfig config)
    {
        if (config.ContainsKeywords?.Count > 0)
        {
            foreach (var val in row.Values)
            {
                if (string.IsNullOrEmpty(val)) continue;
                foreach (var keyword in config.ContainsKeywords)
                {
                    if (val.Contains(keyword)) return true;
                }
            }
        }

        // 空字段判断 - 配置的 Excel 列全部为空
        if (config.EmptyFields?.Count > 0)
        {
            bool allEmpty = true;
            foreach (var excelCol in config.EmptyFields)
            {
                if (row.TryGetValue(excelCol, out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    allEmpty = false;
                    break;
                }
            }
            if (allEmpty) return true;
        }

        return false;
    }

    /// <summary>构建结构化 STG 目标表行</summary>
    private Dictionary<string, object?> BuildTargetTableRow(
        long batchId, int currentRow, string businessKey,
        Dictionary<string, object?> transformedData)
    {
        var rowDict = new Dictionary<string, object?>();

        // 先写入转换后的业务列
        foreach (var kvp in transformedData)
        {
            rowDict[kvp.Key] = kvp.Value;
        }

        // 写入公共列（覆盖同名字段）
        rowDict["FRowHash"] = businessKey;
        rowDict["FImportBatch"] = batchId;
        rowDict["FImportTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        rowDict["F原始行号"] = currentRow;
        rowDict["F处理状态"] = 0;

        return rowDict;
    }

    /// <summary>构建通用表行（原有逻辑 fallback）</summary>
    private Dictionary<string, object?> BuildFallbackRow(
        long batchId, int currentRow, string businessKey,
        Dictionary<string, string> mappedData)
    {
        return new Dictionary<string, object?>
        {
            ["F批次ID"] = batchId,
            ["F原始行号"] = currentRow,
            ["F业务主键"] = businessKey,
            ["F动态数据"] = JsonSerializer.Serialize(mappedData, JsonOptions),
            ["F处理状态"] = 0,
            ["F创建时间"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private async Task FlushBufferAsync(string connStr, string tableName, List<Dictionary<string, object?>> buffer, CancellationToken ct)
    {
        if (buffer.Count == 0) return;
        await _bulkInsertService.BulkInsertTargetAsync(connStr, tableName, new List<Dictionary<string, object?>>(buffer), ct);
        buffer.Clear();
    }


    /// <summary>根据列映射配置从行数据中获取值（精确匹配 + 别名匹配）</summary>
    private static string GetValueByMapping(Dictionary<string, string> row, ColumnMappingItem mapping)
    {
        // 精确匹配 excelColumn
        if (row.TryGetValue(mapping.ExcelColumn, out var val) && !string.IsNullOrEmpty(val))
            return val;

        // 别名精确匹配
        foreach (var alias in mapping.Aliases)
        {
            if (row.TryGetValue(alias, out var aliasVal) && !string.IsNullOrEmpty(aliasVal))
                return aliasVal;
        }

        // 模糊匹配（包含关键字）
        var allKeys = new List<string> { mapping.ExcelColumn };
        allKeys.AddRange(mapping.Aliases);

        foreach (var key in allKeys)
        {
            var match = row.FirstOrDefault(kv => kv.Key.Contains(key) && !string.IsNullOrEmpty(kv.Value));
            if (!string.IsNullOrEmpty(match.Value))
                return match.Value;
        }

        return string.Empty;
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>根据映射配置建立列映射关系（供日志/调试使用）</summary>
    private Dictionary<string, ColumnMappingItem> BuildColumnMap()
    {
        var map = new Dictionary<string, ColumnMappingItem>();
        foreach (var m in _columnMappings)
        {
            map[m.ExcelColumn] = m;
            foreach (var alias in m.Aliases)
                map[alias] = m;
        }
        return map;
    }

    #endregion

    #region Inner Types

    private class ColumnMappingItem
    {
        public string ExcelColumn { get; set; } = "";
        public string DbColumn { get; set; } = "";
        public List<string> Aliases { get; set; } = new();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    #endregion
}

/// <summary>
/// Parser 配置数据对象（替代已废除的 DcFileType 实体）
/// </summary>
public class ParserConfig
{
    public string? FColumnMapping { get; set; }
    public string? FKeyFields { get; set; }
    public string? FDecimalFields { get; set; }
    public string? FTargetTable { get; set; }
    public string? FTransformRules { get; set; }
    public string? FSerialNumberRule { get; set; }
    public string? FTotalRowDetectionRules { get; set; }
    public int FHeaderRowNumber { get; set; } = 1;
}
