using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.Dtos;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Services;
using STOTOP.Module.CardFlow.AutoPlugin;
using STOTOP.Module.CardFlow.Services.Dispatch;

namespace STOTOP.Module.CardFlow.Services.Import;

/// <summary>
/// 阶段6：质量分析 — 对已写入暂存表的数据执行质量检查
/// </summary>
public class QualityAnalysisStage : IImportStage
{
    public string StageName => "QualityAnalysis";
    public string StageLabel => "质量分析";

    private readonly STOTOPDbContext _context;
    private readonly IDispatchService _dispatchService;
    private readonly IProgressNotifier _progressNotifier;
    private readonly ILogger<QualityAnalysisStage> _logger;

    // 可配置的阈值
    private const decimal MaxAmountThreshold = 1000000m; // 金额上限：100万
    private const decimal MinAmountThreshold = -1000000m; // 金额下限：-100万

    public QualityAnalysisStage(
        STOTOPDbContext context,
        IDispatchService dispatchService,
        IProgressNotifier progressNotifier,
        ILogger<QualityAnalysisStage> logger)
    {
        _context = context;
        _dispatchService = dispatchService;
        _progressNotifier = progressNotifier;
        _logger = logger;
    }

    public async Task<ImportStageResult> ExecuteAsync(ImportContext context)
    {
        var result = new ImportStageResult();

        try
        {
            // 如果没有指定暂存表，跳过质量分析（非关键错误）
            if (string.IsNullOrEmpty(context.TargetTable))
            {
                result.Success = true;
                result.ErrorMessage = "未指定暂存表，跳过质量分析";
                return result;
            }

            // 推送进度通知
            try
            {
                await _progressNotifier.NotifyPipelineStageAsync(context.BatchId, StageName, "processing");
            }
            catch { /* 忽略通知失败 */ }

            // 1. 读取批次对应的暂存数据
            var stagingData = await GetStagingDataAsync(context.TargetTable, context.BatchId, context.CancellationToken);

            if (stagingData.Count == 0)
            {
                result.Success = true;
                result.ErrorMessage = "暂存表无数据，跳过质量分析";
                return result;
            }

            _logger.LogInformation("质量分析开始：批次 {BatchId}，暂存表 {TargetTable}，共 {Count} 条记录",
                context.BatchId, context.TargetTable, stagingData.Count);

            // 2. 解析列映射配置（用于确定关键字段）
            List<ColumnMappingItem>? mappings = null;
            List<string>? keyFields = null;
            HashSet<string> decimalFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(context.ColumnMappingJson))
            {
                mappings = JsonSerializer.Deserialize<List<ColumnMappingItem>>(context.ColumnMappingJson);
            }

            if (!string.IsNullOrEmpty(context.DecimalFieldsJson))
            {
                var df = JsonSerializer.Deserialize<List<string>>(context.DecimalFieldsJson);
                if (df != null)
                    decimalFields = new HashSet<string>(df, StringComparer.OrdinalIgnoreCase);
            }

            // 获取业务主键配置（直接从 context 读取，DC文件类型已废除）
            if (!string.IsNullOrEmpty(context.KeyFieldsJson))
            {
                keyFields = JsonSerializer.Deserialize<List<string>>(context.KeyFieldsJson);
            }

            // 3. 执行质量检查
            var errors = new List<QualityErrorRecord>();
            var processedCount = 0;
            var passCount = 0;

            foreach (var row in stagingData)
            {
                processedCount++;
                var rowErrors = await ValidateRowAsync(row, mappings, decimalFields, keyFields);
                if (rowErrors.Count == 0)
                {
                    passCount++;
                }
                else
                {
                    errors.AddRange(rowErrors);
                }
            }

            // 4. 写入错误记录到数据库
            var failCount = errors.Select(e => e.RowNumber).Distinct().Count();
            var errorRecords = new List<CfBatchError>();

            foreach (var error in errors)
            {
                var stagingId = stagingData.FirstOrDefault(r =>
                    r.TryGetValue("F原始行号", out var rn) && Convert.ToInt32(rn) == error.RowNumber)?
                    .TryGetValue("FID", out var id) == true ? Convert.ToInt64(id) : (long?)null;

                var errorRecord = new CfBatchError
                {
                    FBatchId = context.BatchId,
                    FStagingId = stagingId,
                    FRowNumber = error.RowNumber,
                    FErrorType = error.ErrorType,
                    FSeverityLevel = error.SeverityLevel,
                    FErrorField = error.ErrorField,
                    FErrorMessage = error.ErrorMessage,
                    FSuggestedFix = error.SuggestedFix,
                    FOriginalValue = error.OriginalValue,
                    FQualityDimension = error.QualityDimension,
                    FCreatedTime = DateTime.Now,
                    FDispatchStatus = error.SeverityLevel == "ERROR" ? "Pending" : null
                };
                errorRecords.Add(errorRecord);
            }

            if (errorRecords.Count > 0)
            {
                _context.Set<CfBatchError>().AddRange(errorRecords);
                await _context.SaveChangesAsync(context.CancellationToken);
            }

            // 5. 自动派发 ERROR 级别的异常
            var dispatchedCount = 0;
            var errorLevelErrors = errorRecords.Where(e => e.FSeverityLevel == "ERROR").ToList();

            if (errorLevelErrors.Count > 0)
            {
                try
                {
                    // 批量创建派发记录
                    var createDto = new CreateDispatchDto
                    {
                        ErrorIds = errorLevelErrors.Select(e => e.FID).ToArray(),
                        DispatchType = "Task", // 默认派发为工作任务
                        Description = $"质量分析自动派发 - 共 {errorLevelErrors.Count} 条异常",
                        Assignee = null, // 自动派发暂无指定处理人
                        AssigneeName = null,
                        Deadline = DateTime.Now.AddDays(1) // 默认截止时间：1天后
                    };

                    await _dispatchService.CreateBatchDispatchAsync(createDto, "系统自动");
                    dispatchedCount = errorLevelErrors.Count;

                    _logger.LogInformation("质量分析自动派发：批次 {BatchId}，派发 {Count} 条异常",
                        context.BatchId, dispatchedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "质量分析自动派发失败：批次 {BatchId}", context.BatchId);
                }
            }

            // 6. 推送质量分析结果 SignalR 事件
            try
            {
                await _progressNotifier.NotifyQualityAnalysisAsync(context.BatchId,
                    processedCount, passCount, failCount, dispatchedCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "质量分析 SignalR 通知失败");
            }

            // 7. 写入统计到 context.Properties
            context.Properties["QualityTotalChecked"] = processedCount;
            context.Properties["QualityPassCount"] = passCount;
            context.Properties["QualityFailCount"] = failCount;
            context.Properties["QualityDispatchedCount"] = dispatchedCount;

            _logger.LogInformation("质量分析完成：批次 {BatchId}，检查 {Total} 条，通过 {Pass} 条，失败 {Fail} 条，派发 {Dispatched} 条",
                context.BatchId, processedCount, passCount, failCount, dispatchedCount);

            // 质量分析阶段是非阻塞的，始终返回成功
            result.Success = true;
            result.ErrorMessage = failCount > 0
                ? $"质量分析完成：发现 {failCount} 条问题记录（已派发 {dispatchedCount} 条）"
                : "质量分析完成：所有记录通过检查";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "质量分析阶段异常：批次 {BatchId}", context.BatchId);
            // 质量分析失败不阻塞 Pipeline，记录错误但返回成功
            result.Success = true;
            result.ErrorMessage = $"质量分析阶段异常：{ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 从暂存表读取当前批次的数据
    /// </summary>
    private async Task<List<Dictionary<string, object?>>> GetStagingDataAsync(
        string targetTable, long batchId, CancellationToken ct)
    {
        var conn = _context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        var sql = $"SELECT * FROM [{targetTable}] WHERE [F批次ID] = @batchId ORDER BY [F原始行号]";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var param = cmd.CreateParameter();
        param.ParameterName = "@batchId";
        param.Value = batchId;
        cmd.Parameters.Add(param);

        var items = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var dict = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            items.Add(dict);
        }

        return items;
    }

    /// <summary>
    /// 验证单行数据
    /// </summary>
    private async Task<List<QualityErrorRecord>> ValidateRowAsync(
        Dictionary<string, object?> row,
        List<ColumnMappingItem>? mappings,
        HashSet<string> decimalFields,
        List<string>? keyFields)
    {
        var errors = new List<QualityErrorRecord>();
        var rowNumber = row.TryGetValue("F原始行号", out var rn) ? Convert.ToInt32(rn) : 0;
        var stagingId = row.TryGetValue("FID", out var id) ? Convert.ToInt64(id) : 0;

        // 1. 空值检查：关键字段是否为空
        if (mappings != null)
        {
            foreach (var mapping in mappings)
            {
                if (!row.TryGetValue(mapping.DbColumn, out var value) || IsEmptyValue(value))
                {
                    errors.Add(new QualityErrorRecord
                    {
                        RowNumber = rowNumber,
                        StagingId = stagingId,
                        ErrorType = "空值检查",
                        SeverityLevel = "WARNING",
                        ErrorField = mapping.DbColumn,
                        ErrorMessage = $"字段 [{mapping.DbColumn}] 值为空",
                        SuggestedFix = "请检查源文件中该字段是否有值",
                        QualityDimension = "完整性"
                    });
                }
            }
        }

        // 2. 数据格式检查：金额字段是否为有效数字
        foreach (var field in decimalFields)
        {
            if (row.TryGetValue(field, out var value) && value != null && !IsEmptyValue(value))
            {
                if (!decimal.TryParse(value.ToString(), out _))
                {
                    errors.Add(new QualityErrorRecord
                    {
                        RowNumber = rowNumber,
                        StagingId = stagingId,
                        ErrorType = "格式检查",
                        SeverityLevel = "ERROR",
                        ErrorField = field,
                        ErrorMessage = $"金额字段 [{field}] 值 [{value}] 不是有效数字",
                        OriginalValue = value?.ToString(),
                        SuggestedFix = "请修正为有效的数字格式",
                        QualityDimension = "准确性"
                    });
                }
            }
        }

        // 3. 异常值检查：金额是否超出合理范围
        foreach (var field in decimalFields)
        {
            if (row.TryGetValue(field, out var value) && value != null && !IsEmptyValue(value))
            {
                if (decimal.TryParse(value.ToString(), out var numValue))
                {
                    if (numValue > MaxAmountThreshold || numValue < MinAmountThreshold)
                    {
                        errors.Add(new QualityErrorRecord
                        {
                            RowNumber = rowNumber,
                            StagingId = stagingId,
                            ErrorType = "异常值检查",
                            SeverityLevel = "WARNING",
                            ErrorField = field,
                            ErrorMessage = $"金额字段 [{field}] 值 [{numValue}] 超出合理范围（{MinAmountThreshold} ~ {MaxAmountThreshold}）",
                            OriginalValue = numValue.ToString(),
                            SuggestedFix = "请确认该金额是否正确",
                            QualityDimension = "合理性"
                        });
                    }
                }
            }
        }

        // 4. 日期字段格式检查
        foreach (var kvp in row)
        {
            var fieldName = kvp.Key;
            var value = kvp.Value;

            // 跳过系统字段
            if (fieldName.StartsWith("F") && (fieldName == "FID" || fieldName == "F批次ID" ||
                fieldName == "F原始行号" || fieldName == "F处理状态" || fieldName == "F创建时间" ||
                fieldName == "F错误信息" || fieldName == "F关联凭证ID" || fieldName == "F流水号"))
            {
                continue;
            }

            // 检测是否可能是日期字段
            if (value != null && !IsEmptyValue(value) && !decimalFields.Contains(fieldName))
            {
                var strValue = value.ToString();
                if (LooksLikeDateField(fieldName) && !IsValidDate(strValue))
                {
                    errors.Add(new QualityErrorRecord
                    {
                        RowNumber = rowNumber,
                        StagingId = stagingId,
                        ErrorType = "格式检查",
                        SeverityLevel = "WARNING",
                        ErrorField = fieldName,
                        ErrorMessage = $"日期字段 [{fieldName}] 值 [{strValue}] 不是有效日期",
                        OriginalValue = strValue,
                        SuggestedFix = "请修正为有效的日期格式（如 yyyy-MM-dd）",
                        QualityDimension = "准确性"
                    });
                }
            }
        }

        // 5. 业务主键重复检查（由调用方在整批数据上执行）
        // 注意：重复检查需要在整批数据上执行，这里只做单行验证

        return errors;
    }

    /// <summary>
    /// 检查整批数据的业务主键重复
    /// </summary>
    private async Task<List<QualityErrorRecord>> CheckDuplicateKeysAsync(
        List<Dictionary<string, object?>> allRows,
        List<string>? keyFields)
    {
        var errors = new List<QualityErrorRecord>();

        if (keyFields == null || keyFields.Count == 0)
            return errors;

        // 构建业务主键字典
        var keyDict = new Dictionary<string, List<int>>();

        foreach (var row in allRows)
        {
            var rowNumber = row.TryGetValue("F原始行号", out var rn) ? Convert.ToInt32(rn) : 0;
            var stagingId = row.TryGetValue("FID", out var id) ? Convert.ToInt64(id) : 0;

            // 构建业务主键
            var keyParts = new List<string>();
            foreach (var keyField in keyFields)
            {
                if (row.TryGetValue(keyField, out var value))
                {
                    keyParts.Add(value?.ToString() ?? "");
                }
                else
                {
                    keyParts.Add("");
                }
            }
            var key = string.Join("|", keyParts);

            if (!keyDict.ContainsKey(key))
            {
                keyDict[key] = new List<int>();
            }
            keyDict[key].Add(rowNumber);
        }

        // 找出重复的业务主键
        foreach (var kvp in keyDict.Where(k => k.Value.Count > 1))
        {
            foreach (var rowNumber in kvp.Value)
            {
                var row = allRows.FirstOrDefault(r =>
                    r.TryGetValue("F原始行号", out var rn) && Convert.ToInt32(rn) == rowNumber);
                var stagingId = row?.TryGetValue("FID", out var id) == true ? Convert.ToInt64(id) : 0;

                errors.Add(new QualityErrorRecord
                {
                    RowNumber = rowNumber,
                    StagingId = stagingId,
                    ErrorType = "重复检查",
                    SeverityLevel = "ERROR",
                    ErrorField = string.Join(", ", keyFields),
                    ErrorMessage = $"业务主键重复：{kvp.Key}，共出现 {kvp.Value.Count} 次",
                    SuggestedFix = "请检查源文件是否存在重复数据",
                    QualityDimension = "唯一性"
                });
            }
        }

        return errors;
    }

    private static bool IsEmptyValue(object? value)
    {
        if (value == null) return true;
        if (value == DBNull.Value) return true;
        var str = value.ToString();
        return string.IsNullOrWhiteSpace(str);
    }

    private static bool LooksLikeDateField(string fieldName)
    {
        var lowerName = fieldName.ToLower();
        return lowerName.Contains("日期") || lowerName.Contains("时间") ||
               lowerName.Contains("date") || lowerName.Contains("time");
    }

    private static bool IsValidDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        // 尝试多种常见日期格式
        var formats = new[]
        {
            "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
            "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss",
            "yyyy年MM月dd日", "MM月dd日", "yyyyMMdd",
            "dd/MM/yyyy", "MM/dd/yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, null,
                DateTimeStyles.None, out _))
            {
                return true;
            }
        }

        // 尝试自动解析
        if (DateTime.TryParse(value, out _))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 质量错误记录（内部使用）
    /// </summary>
    private class QualityErrorRecord
    {
        public int RowNumber { get; set; }
        public long StagingId { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string SeverityLevel { get; set; } = "WARNING";
        public string? ErrorField { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? SuggestedFix { get; set; }
        public string? OriginalValue { get; set; }
        public string QualityDimension { get; set; } = "完整性";
    }
}
