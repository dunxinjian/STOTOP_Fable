using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.Entities;

namespace STOTOP.Module.CardFlow.Services.Import;

/// <summary>
/// 阶段1：安全检查 — 调用 SecureFileUploadValidator 验证文件安全性
/// </summary>
public class SecurityCheckStage : IImportStage
{
    private readonly SecureFileUploadValidator _validator;

    public SecurityCheckStage(SecureFileUploadValidator validator)
    {
        _validator = validator;
    }

    public string StageName => "SecurityCheck";

    public async Task<ImportStageResult> ExecuteAsync(ImportContext context)
    {
        var result = new ImportStageResult();

        if (context.FileStream == null)
        {
            result.Success = false;
            result.IsCritical = true;
            result.ErrorMessage = "文件流为空，无法进行安全验证";
            return result;
        }

        var validationResult = await _validator.ValidateAsync(context.FileName, context.FileSize, context.FileStream);

        if (!validationResult.IsValid)
        {
            result.Success = false;
            result.IsCritical = true;
            result.ErrorMessage = string.Join("; ", validationResult.Errors);
        }
        else
        {
            result.Success = true;
        }

        return result;
    }
}

/// <summary>
/// 阶段2：类型识别 — 已废除，改用 ExcelInputAgent 规则进行管道匹配
/// </summary>

/// <summary>
/// 阶段3：重复检测 — 计算文件 MD5 哈希，查询是否有相同哈希的批次
/// </summary>
public class DuplicateDetectionStage : IImportStage
{
    private readonly STOTOPDbContext _context;

    public DuplicateDetectionStage(STOTOPDbContext context)
    {
        _context = context;
    }

    public string StageName => "DuplicateDetection";

    public async Task<ImportStageResult> ExecuteAsync(ImportContext context)
    {
        var result = new ImportStageResult();

        if (context.FileStream == null || !context.FileStream.CanSeek)
        {
            result.Success = true;
            result.ErrorMessage = "无法计算文件哈希，跳过重复检测";
            return result;
        }

        // 计算 MD5 哈希
        var originalPosition = context.FileStream.Position;
        context.FileStream.Position = 0;
        using var md5 = MD5.Create();
        var hashBytes = await md5.ComputeHashAsync(context.FileStream, context.CancellationToken);
        var fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        context.FileStream.Position = originalPosition;

        // 保存哈希到 Properties 供后续使用
        context.Properties["FileHash"] = fileHash;

        // 查询是否存在相同哈希的批次
        var existingBatch = await _context.Set<CfBatch>()
            .Where(b => b.FFileHash == fileHash)
            .OrderByDescending(b => b.FCreatedTime)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (existingBatch != null)
        {
            context.Properties["IsDuplicate"] = true;
            context.Properties["DuplicateBatchNo"] = existingBatch.FBatchNo ?? string.Empty;
            result.Success = true; // 非关键错误，仅提醒
            result.ErrorMessage = $"检测到重复文件，已有批次号：{existingBatch.FBatchNo}";
        }
        else
        {
            result.Success = true;
        }

        return result;
    }
}

/// <summary>
/// 阶段2.5：暂存表配置检查 — 确保已配置目标暂存表（通过 context 数据判断）
/// </summary>
public class StagingTableCheckStage : IImportStage
{
    public string StageName => "StagingTableCheck";
    private readonly STOTOPDbContext _context;

    public StagingTableCheckStage(STOTOPDbContext context)
    {
        _context = context;
    }

    public async Task<ImportStageResult> ExecuteAsync(ImportContext context)
    {
        var result = new ImportStageResult();

        // DC文件类型已废除，直接从 context 判断目标表配置
        if (string.IsNullOrEmpty(context.TargetTable))
        {
            result.Success = false;
            result.IsCritical = true;
            result.ErrorMessage = $"未配置目标暂存表，请检查管道配置";
            return result;
        }

        result.Success = true;
        return result;
    }
}
