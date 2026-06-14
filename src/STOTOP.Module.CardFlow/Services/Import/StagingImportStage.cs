using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Services;
using STOTOP.Module.CardFlow.AutoPlugin;
using STOTOP.Module.System.Services;

namespace STOTOP.Module.CardFlow.Services.Import;

/// <summary>
/// 阶段4：导入暂存 — 流式读取 Excel，按列映射写入目标暂存表
/// </summary>
public class StagingImportStage : IImportStage
{
    public string StageName => "StagingImport";

    private readonly ExcelParserService _excelParser;
    private readonly IProgressNotifier _progressNotifier;
    private readonly STOTOPDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public StagingImportStage(
        ExcelParserService excelParser,
        IProgressNotifier progressNotifier,
        STOTOPDbContext dbContext,
        IConfiguration configuration)
    {
        _excelParser = excelParser;
        _progressNotifier = progressNotifier;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<ImportStageResult> ExecuteAsync(ImportContext context)
    {
        // 1. 验证前置条件
        if (string.IsNullOrEmpty(context.TargetTable))
            return new ImportStageResult { Success = false, IsCritical = true, ErrorMessage = "目标暂存表未配置" };
        if (string.IsNullOrEmpty(context.ColumnMappingJson))
            return new ImportStageResult { Success = false, IsCritical = true, ErrorMessage = "列映射未配置" };

        // 2. 解析列映射
        var mappings = JsonSerializer.Deserialize<List<ColumnMappingItem>>(context.ColumnMappingJson);
        if (mappings == null || mappings.Count == 0)
            return new ImportStageResult { Success = false, IsCritical = true, ErrorMessage = "列映射为空" };

        // 3. 解析 decimal 字段列表
        var decimalFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(context.DecimalFieldsJson))
        {
            var df = JsonSerializer.Deserialize<List<string>>(context.DecimalFieldsJson);
            if (df != null)
                decimalFields = new HashSet<string>(df, StringComparer.OrdinalIgnoreCase);
        }

        // 4a. 解析业务主键字段配置
        var keyFields = new List<string>();
        if (!string.IsNullOrEmpty(context.KeyFieldsJson))
        {
            try { keyFields = JsonSerializer.Deserialize<List<string>>(context.KeyFieldsJson) ?? new List<string>(); }
            catch { }
        }

        // 4. 获取数据库连接字符串
        var connStr = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        if (string.IsNullOrEmpty(connStr))
            return new ImportStageResult { Success = false, IsCritical = true, ErrorMessage = "未找到系统数据库连接字符串" };

        // 5. 重置文件流位置
        if (context.FileStream != null && context.FileStream.CanSeek)
            context.FileStream.Position = 0;

        // 6. 估算总行数（简单粗略：-1 表示未知，进度显示绝对数量）
        int totalRows = 0;

        // 7. 流式分批读取并写入
        int processedRows = 0;
        int successRows = 0;
        int failRows = 0;
        int skippedRows = 0;

        // 解析合计行检测配置（只解析一次）
        TotalRowDetectionConfig? totalRowConfig = null;
        if (!string.IsNullOrEmpty(context.TotalRowDetectionConfig))
        {
            try { totalRowConfig = JsonSerializer.Deserialize<TotalRowDetectionConfig>(context.TotalRowDetectionConfig); }
            catch { /* 配置格式错误时忽略 */ }
        }

        await _excelParser.ParseAsync(
            context.FileStream!,
            context.FileName,
            context.HeaderRow,
            context.HeaderRow + 1, // dataStartRow: 默认表头下一行
            1000,
            async (batch, batchIndex) =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                // 构建已映射的 Excel 列名集合（只构建一次）
                var mappedExcelColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in mappings)
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

                // 构建 DataTable
                var dt = new DataTable();
                dt.Columns.Add("F批次ID", typeof(long));
                dt.Columns.Add("F原始行号", typeof(int));
                if (!string.IsNullOrEmpty(context.SerialNumberRule))
                    dt.Columns.Add("F流水号", typeof(string));
                foreach (var m in mappings)
                {
                    if (decimalFields.Contains(m.DbColumn))
                        dt.Columns.Add(m.DbColumn, typeof(decimal));
                    else
                        dt.Columns.Add(m.DbColumn, typeof(string));
                }
                dt.Columns.Add("F其他列数据", typeof(string));
                dt.Columns.Add("F业务主键", typeof(string));
                dt.Columns.Add("FOrgId", typeof(long));
                dt.Columns.Add("F数据作用域ID", typeof(string));
                dt.Columns.Add("F源工作项ID", typeof(long));
                dt.Columns.Add("F已撤销", typeof(bool));

                int rowIndex = 0;
                foreach (var row in batch)
                {
                    // 合计行过滤
                    if (totalRowConfig?.Enabled == true && IsTotalRow(row, totalRowConfig))
                    {
                        skippedRows++;
                        rowIndex++;
                        continue;
                    }

                    var dr = dt.NewRow();
                    dr["F批次ID"] = context.BatchId;
                    dr["F原始行号"] = batchIndex + rowIndex;
                    foreach (var m in mappings)
                    {
                        // 按 excelColumn 和 aliases 查找值
                        string? value = null;
                        if (row.ContainsKey(m.ExcelColumn))
                            value = row[m.ExcelColumn];
                        else if (m.Aliases != null)
                        {
                            foreach (var alias in m.Aliases)
                            {
                                if (row.ContainsKey(alias))
                                {
                                    value = row[alias];
                                    break;
                                }
                            }
                        }

                        if (decimalFields.Contains(m.DbColumn) && !string.IsNullOrEmpty(value))
                        {
                            if (decimal.TryParse(value, out var dv))
                                dr[m.DbColumn] = dv;
                            else
                                dr[m.DbColumn] = DBNull.Value;
                        }
                        else
                        {
                            dr[m.DbColumn] = string.IsNullOrEmpty(value) ? (object)DBNull.Value : value;
                        }
                    }

                    // 流水号处理：根据规则模板替换 {列名} 占位符
                    if (!string.IsNullOrEmpty(context.SerialNumberRule))
                    {
                        var serialNumber = Regex.Replace(context.SerialNumberRule, @"\{(.+?)\}", match =>
                        {
                            var colName = match.Groups[1].Value;
                            // 先尝试从 Excel 原始数据中查找
                            if (row.ContainsKey(colName))
                                return row[colName] ?? "";
                            // 再尝试通过列映射的 dbColumn 反查
                            var mapping = mappings.FirstOrDefault(m => m.DbColumn == colName);
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
                        dr["F流水号"] = string.IsNullOrEmpty(serialNumber) ? DBNull.Value : (object)serialNumber;
                    }

                    // 收集未映射列写入 F其他列数据
                    var unmappedData = new Dictionary<string, string>();
                    foreach (var kv in row)
                    {
                        if (!mappedExcelColumns.Contains(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                        {
                            unmappedData[kv.Key] = kv.Value;
                        }
                    }
                    dr["F其他列数据"] = unmappedData.Count > 0
                        ? JsonSerializer.Serialize(unmappedData, new JsonSerializerOptions { Encoder = global::System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })
                        : DBNull.Value;

                    // 生成业务主键 (SHA256)
                    var keyParts = new StringBuilder();
                    foreach (var keyField in keyFields)
                    {
                        if (row.TryGetValue(keyField, out var kv))
                            keyParts.Append(kv);
                    }
                    dr["F业务主键"] = ComputeSha256(keyParts.ToString());

                    // 填充数据隔离字段
                    dr["FOrgId"] = context.OrgId;
                    dr["F数据作用域ID"] = context.DataScopeId ?? context.BatchId.ToString();
                    dr["F源工作项ID"] = context.WorkItemId.HasValue ? (object)context.WorkItemId.Value : DBNull.Value;
                    dr["F已撤销"] = false;

                    dt.Rows.Add(dr);
                    rowIndex++;
                }

                // SqlBulkCopy 写入
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync(context.CancellationToken);

                StagingTableNameValidator.EnsureSafe(context.TargetTable!);
                using var bulkCopy = new SqlBulkCopy(conn);
                bulkCopy.DestinationTableName = $"[{context.TargetTable}]";
                bulkCopy.BatchSize = 1000;
                bulkCopy.BulkCopyTimeout = 120;

                bulkCopy.ColumnMappings.Add("F批次ID", "F批次ID");
                bulkCopy.ColumnMappings.Add("F原始行号", "F原始行号");
                if (!string.IsNullOrEmpty(context.SerialNumberRule))
                    bulkCopy.ColumnMappings.Add("F流水号", "F流水号");
                foreach (var m in mappings)
                    bulkCopy.ColumnMappings.Add(m.DbColumn, m.DbColumn);
                bulkCopy.ColumnMappings.Add("F其他列数据", "F其他列数据");
                bulkCopy.ColumnMappings.Add("F业务主键", "F业务主键");
                bulkCopy.ColumnMappings.Add("FOrgId", "FOrgId");
                bulkCopy.ColumnMappings.Add("F数据作用域ID", "F数据作用域ID");
                bulkCopy.ColumnMappings.Add("F源工作项ID", "F源工作项ID");
                bulkCopy.ColumnMappings.Add("F已撤销", "F已撤销");

                await bulkCopy.WriteToServerAsync(dt, context.CancellationToken);

                // 统计
                processedRows += dt.Rows.Count;
                successRows += dt.Rows.Count;

                // 推送进度（totalRows=0 时前端按绝对数量显示）
                try
                {
                    await _progressNotifier.NotifyImportProgressAsync(
                        context.BatchId, processedRows, totalRows, StageName);
                }
                catch { /* 进度通知失败不影响主流程 */ }
            },
            context.CancellationToken
        );

        // 8. 持久化批次记录
        var batchEntity = await _dbContext.Set<CfBatch>()
            .AsTracking()
            .FirstOrDefaultAsync(b => b.FID == context.BatchId);
        if (batchEntity != null)
        {
            batchEntity.FActualTargetTable = context.TargetTable;
            await _dbContext.SaveChangesAsync();
        }

        // 9. 写入统计到 context.Properties
        context.Properties["TotalRows"] = processedRows;
        context.Properties["SuccessRows"] = successRows;
        context.Properties["FailRows"] = failRows;
        context.Properties["SkippedRows"] = skippedRows;

        return new ImportStageResult { Success = true };
    }

    private static string ComputeSha256(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static bool IsTotalRow(Dictionary<string, string> row, TotalRowDetectionConfig config)
    {
        // 条件1：包含字符判断 - 任意字段值包含配置的关键字
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

        // 条件2：空字段判断 - 配置的 Excel 列全部为空
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
}

/// <summary>列映射配置项</summary>
public class ColumnMappingItem
{
    [JsonPropertyName("excelColumn")]
    public string ExcelColumn { get; set; } = string.Empty;

    [JsonPropertyName("dbColumn")]
    public string DbColumn { get; set; } = string.Empty;

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}

/// <summary>合计行检测配置</summary>
public class TotalRowDetectionConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("emptyFields")]
    public List<string>? EmptyFields { get; set; }

    [JsonPropertyName("containsKeywords")]
    public List<string>? ContainsKeywords { get; set; }
}
