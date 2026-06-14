using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Services.Interfaces;

namespace STOTOP.Module.CardFlow.Services;

/// <summary>
/// 批次启动恢复服务：应用启动时扫描状态为 0(Parsing) 或 4(Processing) 且 FUpdatedTime
/// 超过10分钟未更新的"卡住"批次，通过 FlowEngineService 重新触发执行。
/// </summary>
public class BatchRecoveryHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BatchRecoveryHostedService> _logger;

    public BatchRecoveryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BatchRecoveryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RecoverStuckBatchesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BatchRecoveryHostedService 启动恢复失败");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RecoverStuckBatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<STOTOPDbContext>();

        // 表存在性保护：首次启动时表可能尚未创建
        if (!await TableExistsAsync(db, "CF批次", cancellationToken))
        {
            _logger.LogInformation("BatchRecoveryHostedService: CF批次表尚未创建，跳过恢复");
            return;
        }

        // 超时阈值：FUpdatedTime 超过10分钟未更新视为卡住
        var cutoff = DateTime.Now.AddMinutes(-10);

        var stuckBatches = await db.Set<CfBatch>()
            .IgnoreQueryFilters()
            .Where(b => !b.FIsRevoked
                && (b.FStatus == 0 || b.FStatus == 4)
                && (b.FUpdatedTime == null || b.FUpdatedTime < cutoff))
            .Select(b => new { b.FID, b.FStatus })
            .ToListAsync(cancellationToken);

        if (stuckBatches.Count == 0)
        {
            _logger.LogInformation("BatchRecoveryHostedService: 无卡住的批次需要恢复");
            return;
        }

        _logger.LogWarning("BatchRecoveryHostedService: 发现 {Count} 个卡住的批次（状态0/4，FUpdatedTime<{Cutoff}），开始恢复",
            stuckBatches.Count, cutoff);

        foreach (var batch in stuckBatches)
        {
            var batchId = batch.FID;
            _logger.LogWarning("BatchRecoveryHostedService: 恢复卡住的批次 BatchId={BatchId}, Status={Status}",
                batchId, batch.FStatus);

            _ = Task.Run(async () =>
            {
                try
                {
                    using var innerScope = _scopeFactory.CreateScope();
                    var db2 = innerScope.ServiceProvider.GetRequiredService<STOTOPDbContext>();
                    var engine = innerScope.ServiceProvider.GetRequiredService<IFlowEngineService>();

                    var cfBatch = await db2.Set<CfBatch>().FirstOrDefaultAsync(b => b.FID == batchId);
                    if (cfBatch == null)
                    {
                        _logger.LogWarning("BatchRecoveryHostedService: 批次 {BatchId} 不存在，跳过恢复", batchId);
                        return;
                    }

                    await engine.ProcessBatchStagesAsync(cfBatch, CancellationToken.None);
                    _logger.LogInformation("BatchRecoveryHostedService: 批次 {BatchId} 恢复执行完成", batchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BatchRecoveryHostedService: 批次 {BatchId} 恢复执行失败", batchId);
                }
            }, CancellationToken.None);
        }
    }

    private static async Task<bool> TableExistsAsync(STOTOPDbContext db, string tableName, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var wasOpen = conn.State == global::System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync(ct);
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = 'dbo') THEN 1 ELSE 0 END";
            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result) == 1;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }
    }
}
