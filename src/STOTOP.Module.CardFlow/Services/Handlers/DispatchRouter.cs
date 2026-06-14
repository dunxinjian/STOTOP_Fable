using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Models;
using STOTOP.Module.CardFlow.Services;
using STOTOP.Module.CardFlow.AutoPlugin;
using STOTOP.Module.Finance.Entities;
using STOTOP.Module.Workflow.DTOs;
using STOTOP.Module.Workflow.Enums;
using STOTOP.Module.Workflow.Services.Interfaces;
using Dapper;

namespace STOTOP.Module.CardFlow.Services.Handlers;

public class DispatchRouter
{
    private readonly STOTOPDbContext _context;
    private readonly ClassificationHandlerFactory _handlerFactory;
    private readonly ILogger<DispatchRouter> _logger;
    private readonly SystemServiceAccountOptions _serviceAccountOptions;
    private readonly IProgressNotifier _progressNotifier;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DispatchRouter(
        STOTOPDbContext context,
        ClassificationHandlerFactory handlerFactory,
        ILogger<DispatchRouter> logger,
        IOptions<SystemServiceAccountOptions> serviceAccountOptions,
        IProgressNotifier progressNotifier,
        IServiceScopeFactory serviceScopeFactory)
    {
        _context = context;
        _handlerFactory = handlerFactory;
        _logger = logger;
        _serviceAccountOptions = serviceAccountOptions.Value;
        _progressNotifier = progressNotifier;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>对分类结果执行调度路由</summary>
    public async Task DispatchAsync(ClassificationResult result)
    {
        _logger.LogInformation("开始调度路由: 批次 {BatchId}, 共 {Count} 条分类项",
            result.BatchId, result.Items.Count);

        foreach (var classification in result.Items)
        {
            // 1. 直接按 DispatchRuleId 加载规则
            var rule = await _context.Set<CfDispatchRule>().FindAsync(classification.DispatchRuleId);
            if (rule == null)
            {
                _logger.LogWarning("派发规则 {RuleId} 不存在，跳过", classification.DispatchRuleId);
                continue;
            }

            if (rule.FStatus != 1)
            {
                _logger.LogDebug("派发规则 {RuleId} ({RuleName}) 已禁用，跳过", rule.FID, rule.FRuleName);
                continue;
            }

            _logger.LogInformation("派发规则 {RuleId} ({RuleName}) -> Handler: {HandlerType}",
                rule.FID, rule.FRuleName, rule.FHandlerType);

            // 2. 通过工厂创建Handler
            var handler = _handlerFactory.Create(rule.FHandlerType);
            if (handler == null)
            {
                _logger.LogWarning("规则 {RuleId}: 无法创建 Handler 类型 {HandlerType}",
                    rule.FID, rule.FHandlerType);
                continue;
            }

            // 3. 构建上下文（含组织、用户、财务信息）
            long orgId = _serviceAccountOptions.OrgId;
            long creatorId = _serviceAccountOptions.UserId;

            // 从规则配置中读取日期字段名（默认 F业务日期）
            string dateField = "F业务日期";
            if (!string.IsNullOrEmpty(rule.FHandlerConfigJson))
            {
                try
                {
                    using var configDoc = JsonDocument.Parse(rule.FHandlerConfigJson);
                    if (configDoc.RootElement.TryGetProperty("dateField", out var df) && df.ValueKind == JsonValueKind.String)
                        dateField = df.GetString() ?? "F业务日期";
                }
                catch { }
            }

            // 优先从暂存表获取业务日期来确定会计期间
            var businessDate = await GetBusinessDateFromStagingTableAsync(result.BatchId, result.TargetTable, dateField);
            var queryDate = businessDate ?? DateTime.Now;

            long? periodId = null;
            long? accountSetId = null;
            try
            {
                var period = await _context.Set<FinAccountPeriod>()
                    .Where(p => p.FStartDate <= queryDate && p.FEndDate >= queryDate && p.FIsClosed == 0)
                    .OrderByDescending(p => p.FStartDate)
                    .FirstOrDefaultAsync();

                // 期间不存在则自动创建（双检查 + 捕获冲突重查）
                if (period == null)
                {
                    // 查默认账套
                    var defaultAccountSet = await _context.Set<FinAccountSet>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.FIsDefault && a.FStatus == 1);
                    long targetAccountSetId = defaultAccountSet?.FID ?? 0;
                    long targetOrgId = defaultAccountSet?.FOrgId ?? 0;

                    if (targetAccountSetId > 0)
                    {
                        var startDate = new DateTime(queryDate.Year, queryDate.Month, 1);
                        var endDate = startDate.AddMonths(1).AddDays(-1);
                        try
                        {
                            period = new FinAccountPeriod
                            {
                                FYear = queryDate.Year,
                                FPeriodNo = queryDate.Month,
                                FStartDate = startDate,
                                FEndDate = endDate,
                                FIsClosed = 0,
                                FStatus = 1,
                                FAccountSetId = targetAccountSetId,
                                FOrgId = targetOrgId,
                                FCreatedTime = DateTime.Now,
                                FUpdatedTime = DateTime.Now
                            };
                            _context.Set<FinAccountPeriod>().Add(period);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("自动创建会计期间: {Year}年第{Month}期, 账套={AccountSetId}",
                                period.FYear, period.FPeriodNo, period.FAccountSetId);
                            _logger.LogWarning("账套 {AccountSetId} 缺失 {Year}-{Month} 会计期间，已自动创建。如存在前序期间缺失，请手动执行账套初始化或使用年度期间创建功能补齐。",
                                targetAccountSetId, queryDate.Year, queryDate.Month);
                        }
                        catch (DbUpdateException ex)
                        {
                            // 唯一约束冲突 → 其他线程已创建成功，分离失败实体并重新查询
                            _logger.LogInformation(ex, "会计期间并发创建冲突，重新查询已有记录");
                            if (period != null) _context.Entry(period).State = EntityState.Detached;
                            period = await _context.Set<FinAccountPeriod>()
                                .Where(p => p.FStartDate <= queryDate && p.FEndDate >= queryDate && p.FIsClosed == 0)
                                .OrderByDescending(p => p.FStartDate)
                                .FirstOrDefaultAsync();
                        }
                    }
                    else
                    {
                        _logger.LogWarning("无有效的目标账套 ID（规则: {RuleId}），跳过会计期间创建", rule.FID);
                        periodId = null;
                    }
                }

                if (period != null)
                {
                    periodId = period.FID;
                    accountSetId = period.FAccountSetId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "获取会计期间失败（查询日期: {QueryDate}），PeriodId/AccountSetId 将为 null", queryDate);
            }

            var context = new HandlerContext
            {
                BatchId = result.BatchId,
                TargetTable = result.TargetTable,
                Classification = classification,
                HandlerConfig = rule.FHandlerConfigJson,
                OrgId = orgId,
                CreatorId = creatorId,
                PeriodId = periodId,
                AccountSetId = accountSetId,
                BusinessDate = businessDate
            };

            // 4. 查询派发结果记录ID（用于推送）
            long dispatchResultId = 0;
            try
            {
                var dispatchEntity = await _context.Set<CfSystemDispatchResult>()
                    .FirstOrDefaultAsync(r => r.FBatchId == result.BatchId && r.FDispatchRuleId == classification.DispatchRuleId);
                if (dispatchEntity != null) dispatchResultId = dispatchEntity.FID;
            }
            catch { }

            // 4b. 创建 WF WorkItem 追踪派发任务
            try
            {
                await CreateDispatchWorkItemAsync(result.BatchId, rule.FRuleName ?? rule.FHandlerType ?? "", orgId, creatorId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "创建派发 WorkItem 失败，继续执行: BatchId={BatchId}, Rule={RuleName}", result.BatchId, rule.FRuleName);
            }

            // 5. 更新派发结果状态为"处理中" + 推送
            await UpdateDispatchResultStatusAsync(result.BatchId, classification.DispatchRuleId, 1, null);
            try { await _progressNotifier.NotifyDispatchItemAsync(result.BatchId, dispatchResultId, rule.FRuleName ?? "", rule.FHandlerType ?? "", 1, null); } catch { }

            // 6. 执行（区分同步/异步）
            if (rule.FIsAsync)
            {
                var ruleId = rule.FID;
                var handlerType = rule.FHandlerType;
                var batchId = result.BatchId;
                var capturedDispatchRuleId = classification.DispatchRuleId;
                var capturedDispatchResultId = dispatchResultId;
                var capturedRuleName = rule.FRuleName ?? "";
                var capturedHandlerType = rule.FHandlerType ?? "";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var handlerResult = await handler.HandleAsync(context);
                        _logger.LogInformation("规则 {RuleId} 异步执行完成: {HandlerType} => {Success}, {Message}",
                            ruleId, handlerType, handlerResult.Success, handlerResult.Message);

                        var asyncStatus = handlerResult.Success ? 2 : 3;
                        var resultJson = JsonSerializer.Serialize(new
                        {
                            handlerResult.Success,
                            handlerResult.Message,
                            HandlerType = capturedHandlerType,
                            ExecuteTime = DateTime.Now
                        });
                        await UpdateDispatchResultInNewScopeAsync(batchId, capturedDispatchRuleId, asyncStatus, resultJson);
                        try { await _progressNotifier.NotifyDispatchItemAsync(batchId, capturedDispatchResultId, capturedRuleName, capturedHandlerType, asyncStatus, handlerResult.Message); } catch { }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "规则 {RuleId} 异步执行 {HandlerType} 失败", ruleId, handlerType);
                        var failJson = JsonSerializer.Serialize(new
                        {
                            Success = false,
                            Message = ex.Message,
                            HandlerType = capturedHandlerType,
                            ExecuteTime = DateTime.Now
                        });
                        await UpdateDispatchResultInNewScopeAsync(batchId, capturedDispatchRuleId, 3, failJson);
                        try { await _progressNotifier.NotifyDispatchItemAsync(batchId, capturedDispatchResultId, capturedRuleName, capturedHandlerType, 3, ex.Message); } catch { }
                    }
                });
            }
            else
            {
                try
                {
                    var handlerResult = await handler.HandleAsync(context);

                    // 更新派发结果状态和结果
                    var status = handlerResult.Success ? 2 : 3; // 2已处理 3处理失败
                    var resultJson = JsonSerializer.Serialize(new
                    {
                        handlerResult.Success,
                        handlerResult.Message,
                        HandlerType = rule.FHandlerType,
                        ExecuteTime = DateTime.Now
                    });

                    await UpdateDispatchResultStatusAsync(result.BatchId, classification.DispatchRuleId, status, resultJson);
                    try { await _progressNotifier.NotifyDispatchItemAsync(result.BatchId, dispatchResultId, rule.FRuleName ?? "", rule.FHandlerType ?? "", status, handlerResult.Message); } catch { }

                    _logger.LogInformation("规则 {RuleId} 同步执行完成: {HandlerType} => {Success}, {Message}",
                        rule.FID, rule.FHandlerType, handlerResult.Success, handlerResult.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "规则 {RuleId} 同步执行 {HandlerType} 失败", rule.FID, rule.FHandlerType);

                    var failJson = JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = ex.Message,
                        HandlerType = rule.FHandlerType,
                        ExecuteTime = DateTime.Now
                    });

                    await UpdateDispatchResultStatusAsync(result.BatchId, classification.DispatchRuleId, 3, failJson);
                    try { await _progressNotifier.NotifyDispatchItemAsync(result.BatchId, dispatchResultId, rule.FRuleName ?? "", rule.FHandlerType ?? "", 3, ex.Message); } catch { }
                }
            }
        }

        _logger.LogInformation("调度路由完成: 批次 {BatchId}", result.BatchId);
    }

    /// <summary>从暂存表查询指定日期字段</summary>
    private async Task<DateTime?> GetBusinessDateFromStagingTableAsync(long batchId, string targetTable, string dateField)
    {
        try
        {
            var sql = $"SELECT TOP 1 [{dateField}] FROM [{targetTable}] WHERE [F批次ID] = @BatchId AND [{dateField}] IS NOT NULL ORDER BY [{dateField}]";
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            var businessDate = await SqlMapper.ExecuteScalarAsync<DateTime?>(connection, sql, new { BatchId = batchId });
            return businessDate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法从暂存表 {Table} 获取日期字段 {DateField}，将使用当前时间", targetTable, dateField);
            return null;
        }
    }

    /// <summary>在新 scope 中更新派发结果状态（用于 Task.Run 等异步并发场景，避免 DbContext 线程安全问题）</summary>
    private async Task UpdateDispatchResultInNewScopeAsync(long batchId, long dispatchRuleId, int status, string? resultJson)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<STOTOPDbContext>();

            var entity = await dbContext.Set<CfSystemDispatchResult>()
                .FirstOrDefaultAsync(r => r.FBatchId == batchId && r.FDispatchRuleId == dispatchRuleId);

            if (entity != null)
            {
                entity.FProcessingStatus = status;
                if (resultJson != null)
                    entity.FProcessingResult = resultJson;

                dbContext.Set<CfSystemDispatchResult>().Update(entity);
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步更新派发结果状态失败: 批次 {BatchId}, 规则 {RuleId}", batchId, dispatchRuleId);
        }
    }

    /// <summary>更新派发结果的处理状态</summary>
    private async Task UpdateDispatchResultStatusAsync(long batchId, long dispatchRuleId, int status, string? resultJson)
    {
        try
        {
            var entity = await _context.Set<CfSystemDispatchResult>()
                .FirstOrDefaultAsync(r => r.FBatchId == batchId && r.FDispatchRuleId == dispatchRuleId);

            if (entity != null)
            {
                entity.FProcessingStatus = status;
                if (resultJson != null)
                    entity.FProcessingResult = resultJson;

                _context.Set<CfSystemDispatchResult>().Update(entity);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新派发结果状态失败: 批次 {BatchId}, 规则 {RuleId}", batchId, dispatchRuleId);
        }
    }

    /// <summary>创建派发任务对应的 WF WorkItem</summary>
    private async Task CreateDispatchWorkItemAsync(long batchId, string ruleName, long orgId, long creatorId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var workItemService = scope.ServiceProvider.GetRequiredService<IWorkItemService>();
        await workItemService.CreateAsync(new CreateWorkItemRequest
        {
            OrgId = orgId,
            Title = $"{ruleName} - 待处理",
            Description = $"批次#{batchId} 的 {ruleName} 派发结果需要处理",
            Type = (int)WorkItemType.Task,
            Source = (int)WorkItemSource.Pipeline,
            Priority = (int)WorkItemPriority.Normal,
            DataScopeId = batchId.ToString(),
            CreatorId = creatorId,
            Module = "DataCenter",
            BizType = ruleName,
            BizId = batchId,
            DetailRoute = $"/datacenter/upload?batch={batchId}&type={Uri.EscapeDataString(ruleName)}",
            AutoDispatch = true
        });
    }
}
