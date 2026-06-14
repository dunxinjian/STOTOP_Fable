namespace STOTOP.Module.CardFlow.AutoPlugin;

/// <summary>
/// CFAutoPlugin 向批次宿主（CardFlow）回写状态的回调接口。
/// 所有参数使用 BCL 原生类型，不引用 CardFlow 程序集，避免循环依赖。
/// 迁移自 CFAutoPlugin.Abstractions
/// </summary>
public interface IBatchProgressCallback
{
    /// <summary>更新批次状态（status 对应 CfBatchStatus/CfPipelineStatus 常量值）</summary>
    Task UpdateStatusAsync(int batchId, int status, string? errorMessage = null);
    /// <summary>更新批次进度</summary>
    Task UpdateProgressAsync(int batchId, int percent, string pluginName, string stepName);
    /// <summary>设置批次错误</summary>
    Task SetErrorAsync(int batchId, string errorMessage);
}
