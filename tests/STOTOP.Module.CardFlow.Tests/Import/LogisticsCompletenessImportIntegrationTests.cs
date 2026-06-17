using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.AutoPlugin;
using STOTOP.Module.CardFlow.AutoPlugin.Implementations;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Services.Import;
using STOTOP.Module.CardFlow.Services.Import.TransformEngine;
using STOTOP.Module.Finance.Entities;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.System.Entities;
using STOTOP.WebAPI.Data.Seeders;
using Xunit;

namespace STOTOP.Module.CardFlow.Tests.Import;

// STOTOP.Module.Task / STOTOP.Module.System 会遮蔽 BCL 命名空间，命名空间内 alias 恢复
using Task = global::System.Threading.Tasks.Task;

/// <summary>
/// Task B0：接入 STG申通_物流完整性明细 的真库集成测试（连开发测试库 stotop）。
/// 1) Migrate 幂等建表 + 种子规则 3101 / 流程 2301 / 版本 2301 / 首节点 5101。
/// 2) 真导入 excel (未到件).xls：构造真 CfBatch + ExcelInputPlugin 直接 ExecuteAsync（同步执行，
///    不依赖异步 Channel 后台消费）。断言本批 15 行、问题类型全为「未到件」、运单号非空。
/// 用完按 F批次ID 清理本测试数据，避免污染累积。
/// </summary>
public class LogisticsCompletenessImportIntegrationTests
{
    private const long OrgId = 192;
    private const string 未到件Xls =
        @"E:\STOTOP_Fable\Taicang\网点质控数据\申通网点\申通数据明细\excel (未到件).xls";

    private const string TableName = "STG申通_物流完整性明细";

    private static STOTOPDbContext CreateDbContext(string conn)
    {
        STOTOPDbContext.RegisterModuleAssembly(typeof(CfCard).Assembly);
        STOTOPDbContext.RegisterModuleAssembly(typeof(OaExpenseRequest).Assembly);
        STOTOPDbContext.RegisterModuleAssembly(typeof(SysUser).Assembly);
        STOTOPDbContext.RegisterModuleAssembly(typeof(FinVoucher).Assembly);

        var options = new DbContextOptionsBuilder<STOTOPDbContext>()
            .UseSqlServer(conn)
            .Options;
        return new STOTOPDbContext(options);
    }

    private sealed class NoopProgressReporter : IPluginProgressReporter
    {
        public Task ReportProgressAsync(long batchId, int processedRows, int totalRows, string? currentStep = null) => Task.CompletedTask;
        public Task ReportErrorAsync(long batchId, int rowIndex, string errorMessage) => Task.CompletedTask;
        public Task ReportCompletedAsync(long batchId, bool success, string? message = null) => Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Migrate_CreatesTable_And_SeedsRule_And_RealImport_15Rows()
    {
        Skip.IfNot(TestSqlConnection.IsAvailable, "无可用 SQL 连接（未设 STOTOP_TEST_CONNECTION 且主树 db-connections.json 不可读），跳过集成测试");
        Skip.IfNot(File.Exists(未到件Xls), $"测试数据文件不存在: {未到件Xls}");

        var conn = TestSqlConnection.GetConnectionString()!;
        // 确保插件内部 DbConnectionsHelper（读 BaseDirectory）也解析到同一 stotop 库
        TestSqlConnection.EnsureSystemConnectionFile();

        await using var db = CreateDbContext(conn);

        // 可达性兜底：连不上则跳过而非失败
        try { await db.Database.OpenConnectionAsync(); await db.Database.CloseConnectionAsync(); }
        catch (Exception ex) { Skip.If(true, $"SQL 不可达，跳过：{ex.Message}"); }

        // ── 1. 迁移（幂等建表 + 种子） ──
        CardFlowSeeder.Migrate(db);

        // 断言：表存在
        var tableExists = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = N'STG申通_物流完整性明细'")
            .SingleAsync();
        Assert.True(tableExists > 0, "Migrate 后应存在表 [STG申通_物流完整性明细]");

        // 断言：规则 3101 存在
        var ruleExists = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM [CF自动插件_规则] WHERE [FID] = 3101")
            .SingleAsync();
        Assert.True(ruleExists > 0, "Migrate 后应存在 CF自动插件_规则 FID=3101");

        // ── 2. 真导入 ──
        long batchId = 0;
        try
        {
            // 构造真 CfBatch（指向 未到件 文件，组织=192，绑定流程 2301）
            var batch = new CfBatch
            {
                FFlowDefinitionId = 2301,
                FOrgId = OrgId,
                FTriggeredById = 1,
                FTriggeredTime = DateTime.Now,
                FTriggerType = "fileUpload",
                FFilePath = 未到件Xls,
                FFileName = "excel (未到件).xls",
                FBatchNo = $"TESTB0-{Guid.NewGuid():N}",
                FStatus = CfBatchStatus.Parsing,
                FUploadMethod = "auto",
                FCreatedTime = DateTime.Now,
            };
            db.Set<CfBatch>().Add(batch);
            await db.SaveChangesAsync();
            batchId = batch.FID;

            // 先清掉可能残留的本批 STG 行（保证可重复运行）
            await DeleteStgByBatchAsync(conn, batchId);

            var config = new ConfigurationBuilder().Build();
            var plugin = new ExcelInputPlugin(
                new ExcelParserService(),
                new EfCoreBulkInsertService(db),
                new JintTransformEngine(NullLogger<JintTransformEngine>.Instance),
                new NoopProgressReporter(),
                db,
                NullLogger<ExcelInputPlugin>.Instance,
                config);

            var ctx = new PluginContext
            {
                BatchId = batchId,
                StageDefinitionId = 5101,
                PluginRuleId = 3101,
                OrgId = OrgId,
                Services = null!,
            };

            var result = await plugin.ExecuteAsync(ctx);
            Assert.True(result.Success, $"导入应成功，实际 Message={result.Message}");

            // ── 3. 断言：本批 15 行、问题类型全「未到件」、运单号非空 ──
            var rowCount = await CountAsync(conn,
                $"SELECT COUNT(*) FROM [{TableName}] WHERE [F批次ID] = @b", batchId);
            Assert.Equal(15, rowCount);

            var wrongType = await CountAsync(conn,
                $"SELECT COUNT(*) FROM [{TableName}] WHERE [F批次ID] = @b AND ([F问题类型] IS NULL OR [F问题类型] <> N'未到件')", batchId);
            Assert.Equal(0, wrongType);

            var emptyWaybill = await CountAsync(conn,
                $"SELECT COUNT(*) FROM [{TableName}] WHERE [F批次ID] = @b AND ([F运单号] IS NULL OR LTRIM(RTRIM([F运单号])) = N'')", batchId);
            Assert.Equal(0, emptyWaybill);
        }
        finally
        {
            // 清理本测试批次数据（STG 行 + CfBatch 行），避免污染累积
            if (batchId > 0)
            {
                try { await DeleteStgByBatchAsync(conn, batchId); } catch { /* ignore */ }
                try
                {
                    await using var cleanup = CreateDbContext(conn);
                    var b = await cleanup.Set<CfBatch>().FirstOrDefaultAsync(x => x.FID == batchId);
                    if (b != null)
                    {
                        cleanup.Set<CfBatch>().Remove(b);
                        await cleanup.SaveChangesAsync();
                    }
                }
                catch { /* ignore */ }
            }
        }
    }

    private static async Task DeleteStgByBatchAsync(string conn, long batchId)
    {
        await using var c = new SqlConnection(conn);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = $"DELETE FROM [{TableName}] WHERE [F批次ID] = @b";
        cmd.Parameters.AddWithValue("@b", batchId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountAsync(string conn, string sql, long batchId)
    {
        await using var c = new SqlConnection(conn);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@b", batchId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
}
