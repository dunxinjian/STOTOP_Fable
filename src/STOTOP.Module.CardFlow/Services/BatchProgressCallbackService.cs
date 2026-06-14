using Microsoft.EntityFrameworkCore;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.AutoPlugin;

namespace STOTOP.Module.CardFlow.Services;

/// <summary>
/// CFAutoPlugin IBatchProgressCallback 的 CardFlow 实现
/// 负责将 AutoPlugin 执行进度回写到 CfBatch 实体
/// </summary>
public class BatchProgressCallbackService : IBatchProgressCallback
{
    private readonly STOTOPDbContext _db;

    public BatchProgressCallbackService(STOTOPDbContext db)
    {
        _db = db;
    }

    public async Task UpdateStatusAsync(int batchId, int status, string? errorMessage = null)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE CF批次 SET F状态 = {0}, F错误信息 = {1}, F更新时间 = {2} WHERE FID = {3}",
            status, errorMessage ?? (object)DBNull.Value, DateTime.Now, batchId);
    }

    public async Task UpdateProgressAsync(int batchId, int percent, string pluginName, string stepName)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE CF批次 SET F状态 = {0}, F更新时间 = {1} WHERE FID = {2}",
            percent, DateTime.Now, batchId);
    }

    public async Task SetErrorAsync(int batchId, string errorMessage)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE CF批次 SET F错误信息 = {0}, F更新时间 = {1} WHERE FID = {2}",
            errorMessage, DateTime.Now, batchId);
    }
}
