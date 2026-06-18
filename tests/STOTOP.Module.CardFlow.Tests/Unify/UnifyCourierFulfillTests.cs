using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.AutoPlugin;
using STOTOP.Module.CardFlow.AutoPlugin.Implementations;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Services;
using STOTOP.Module.CardFlow.Services.Import;
using STOTOP.Module.CardFlow.Services.Import.TransformEngine;
using STOTOP.Module.Express.Entities;
using STOTOP.Module.Finance.Entities;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.Quality.Entities;
using STOTOP.Module.Quality.Services.Unification;
using STOTOP.Module.System.Entities;
using STOTOP.WebAPI.Data.Seeders;
using Xunit;
using Xunit.Abstractions;

namespace STOTOP.Module.CardFlow.Tests.Unify;

// STOTOP.Module.Task / STOTOP.Module.System 会遮蔽 BCL Task 命名空间，命名空间内 alias 恢复
using Task = global::System.Threading.Tasks.Task;

/// <summary>
/// Plan2 Phase C — C1：归一服务「员工指标类（小件员履约指标 → 员工日质量指标）」集成测试（连开发测试库 stotop）。
///
/// 安排：EnsureTables 建 QL 表 + CardFlowSeeder.Migrate 建 STG 表；用规则 3124 把小件员履约 Excel（59 行）
///   导入 STG申通_小件员履约指标(orgId=192)；种子 ExpNetworkPoint(FCode=320288, FFullName=江苏太仓市城区公司)；
///   取文件首条脏名「城区吴健304」种 ExpSalesman(工号3209999/网点320288) + ExpSalesmanAlias(脏名→3209999)，
///   使该员工别名可匹配（Status=2，建指标）；其余 58 个小件员无别名/无工号 → 未匹配（不建指标）。
/// 行为：svc.UnifyShentongAsync(192)（泛化分发同时跑事件类与员工指标类；本测试断言员工指标类产物）。
/// 断言：QL申通_员工日质量指标 为匹配员工建 ≥1 行（工号3209999、F业务日期=批次日、派签率/客诉等有值、F员工ID 填充）；
///   未匹配员工不建指标但 result.EmployeeUnmatched>0；再跑一次 → 该员工指标行数不变（幂等）。
/// 清理：try/finally 删本测试 org 的员工日指标 + STG 行 + CfBatch + 种子（业务员/别名/网点）。
/// </summary>
[Collection("StotopRealDb")] // 串行化：与其它真库集成测试共享 stotop
public class UnifyCourierFulfillTests
{
    private const long OrgId = 192;
    private const string NetCode = "320288";
    private const string NetFullName = "江苏太仓市城区公司";
    private const string EmpNo = "3209999";
    private const string DirtyName = "城区吴健304";  // 文件首条数据行的 F所属小件员 脏名
    private const string MetricTable = "QL申通_员工日质量指标";
    private const string StgTable = "STG申通_小件员履约指标";

    private static readonly string 展示页Dir =
        @"E:\STOTOP_Fable\Taicang\网点质控数据\申通网点\申通数据展示页";
    private const string CourierFile = "小件员履约指标历史数据导出_导出任务_20260617_109361182.xlsx";

    private readonly ITestOutputHelper _log;
    public UnifyCourierFulfillTests(ITestOutputHelper log) => _log = log;

    private static void RegisterModules()
    {
        STOTOPDbContext.RegisterModuleAssembly(typeof(CfCard).Assembly);
        STOTOPDbContext.RegisterModuleAssembly(typeof(OaExpenseRequest).Assembly);
        STOTOPDbContext.RegisterModuleAssembly(typeof(SysUser).Assembly);
        STOTOPDbContext.RegisterModuleAssembly(typeof(FinVoucher).Assembly);
        STOTOPDbContext.RegisterModuleAssembly(typeof(ExpSalesman).Assembly);
        STOTOPDbContext.RegisterModuleAssembly(typeof(QlException).Assembly);
    }

    private static STOTOPDbContext CreateDbContext(string conn)
    {
        RegisterModules();
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

    /// <summary>
    /// 快照样例文件到临时目录（源文件可能被 Excel 打开，插件 FileShare.Read 会被独占锁拒绝）。
    /// </summary>
    private static string Snapshot(string srcPath)
    {
        var dir = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), "stqc_unify_snap");
        Directory.CreateDirectory(dir);
        var dst = global::System.IO.Path.Combine(dir, global::System.IO.Path.GetFileName(srcPath));
        using (var src = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var outFs = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            src.CopyTo(outFs);
        }
        return dst;
    }

    [SkippableFact]
    public async Task Unify_CourierFulfill_BuildsEmployeeMetric_OnlyWhenMatched_Idempotent()
    {
        Skip.IfNot(TestSqlConnection.IsAvailable, "无可用 SQL 连接，跳过集成测试");
        var conn = TestSqlConnection.GetConnectionString()!;
        TestSqlConnection.EnsureSystemConnectionFile();

        var path = global::System.IO.Path.Combine(展示页Dir, CourierFile);
        Skip.IfNot(File.Exists(path), $"样例文件不存在: {path}");

        await using var db = CreateDbContext(conn);
        try { await db.Database.OpenConnectionAsync(); await db.Database.CloseConnectionAsync(); }
        catch (Exception ex) { Skip.If(true, $"SQL 不可达，跳过：{ex.Message}"); }

        // 建 STG 表 + QL 表
        CardFlowSeeder.Migrate(db);
        QualityUnifySeeder.EnsureTables(db);

        long batchId = 0;
        bool seededNetwork = false, seededSalesman = false, seededAlias = false;
        try
        {
            // 预清：删除可能残留的本 org 员工日指标（保证可复现）
            await CleanupMetricAsync(conn);

            // 种子主数据：网点（名称匹配目标）+ 业务员（工号3209999/网点320288）+ 别名（脏名→工号）
            seededNetwork = await EnsureNetworkAsync(db);
            seededSalesman = await EnsureSalesmanAsync(db);
            seededAlias = await EnsureAliasAsync(db);

            // ── 导入 STG（59 行，规则 3124 / 流程 2324 / 首节点 5124，headerRow=2 由规则配置）──
            (batchId, var importResult) = await ImportOnceAsync(db, conn, path, 2324, 5124, 3124);
            Assert.True(importResult.Success, $"导入应成功，实际 Message={importResult.Message}");
            var stgRows = await CountAsync(conn, $"SELECT COUNT(*) FROM [{StgTable}] WHERE [F批次ID] = @b", ("@b", batchId));
            _log.WriteLine($"[STG] 导入行数 = {stgRows}（期望 59）");
            Assert.Equal(59, stgRows);

            // 批次创建日（业务日期取该日）
            var batchDay = await ScalarDateAsync(conn, "SELECT CAST([F创建时间] AS DATE) FROM [CF批次] WHERE [FID] = @b", ("@b", batchId));

            // ── 行为：归一（泛化分发；员工指标类路径产出）──
            var svc = new QualityUnificationService(db, new MasterDataMatcher(db));
            var result = await svc.UnifyShentongAsync(OrgId);
            _log.WriteLine($"[首跑] EmpMetricUpserts={result.EmployeeMetricUpserts} EmpUnmatched={result.EmployeeUnmatched} NetUnmatched={result.NetworkUnmatched}");

            // ── 断言：匹配员工建了 ≥1 行 ──
            var metricRows = await CountAsync(conn,
                $"SELECT COUNT(*) FROM [{MetricTable}] WHERE [FOrgId] = @org AND [F员工工号] = @no AND [F业务日期] = @d AND [F网点编码] = @code",
                ("@org", OrgId), ("@no", EmpNo), ("@d", batchDay), ("@code", NetCode));
            _log.WriteLine($"[指标] 匹配员工 {EmpNo} 行数 = {metricRows}（期望 ≥1）");
            Assert.True(metricRows >= 1, $"匹配员工应建 ≥1 行指标，实际 {metricRows}");

            // 该行字段：F员工ID 填充、派签率/客诉率/当日派签量 有值（来自源映射）
            var fieldsOk = await CountAsync(conn,
                $@"SELECT COUNT(*) FROM [{MetricTable}]
                   WHERE [FOrgId] = @org AND [F员工工号] = @no AND [F业务日期] = @d AND [F网点编码] = @code
                     AND [F员工ID] IS NOT NULL AND [F当日派签率] IS NOT NULL AND [F当日派签量] IS NOT NULL",
                ("@org", OrgId), ("@no", EmpNo), ("@d", batchDay), ("@code", NetCode));
            _log.WriteLine($"[指标] 字段填充(F员工ID/派签率/派签量非空)行数 = {fieldsOk}");
            Assert.True(fieldsOk >= 1, "匹配员工指标行应填充 F员工ID + 派签率/派签量");

            // F员工姓名原文 = 脏名原文
            var nameOk = await CountAsync(conn,
                $"SELECT COUNT(*) FROM [{MetricTable}] WHERE [FOrgId] = @org AND [F员工工号] = @no AND [F员工姓名原文] = @raw",
                ("@org", OrgId), ("@no", EmpNo), ("@raw", DirtyName));
            Assert.True(nameOk >= 1, "F员工姓名原文 应保留脏名原文");

            // ── 未匹配员工不建指标，但计数 > 0（其余 58 个小件员无别名/无工号）──
            Assert.True(result.EmployeeUnmatched > 0, $"应有未匹配员工计数，实际 {result.EmployeeUnmatched}");
            // 本批仅匹配到的工号（3209999）应是唯一建指标的工号
            var distinctEmps = await CountAsync(conn,
                $"SELECT COUNT(DISTINCT [F员工工号]) FROM [{MetricTable}] WHERE [FOrgId] = @org AND [F来源批次ID] = @b",
                ("@org", OrgId), ("@b", batchId));
            _log.WriteLine($"[指标] 本批建指标的不同工号数 = {distinctEmps}（期望 1）");
            Assert.Equal(1, distinctEmps);

            // ── 幂等：再跑一次 → 该员工指标行数不变 ──
            var countBefore = await CountAsync(conn,
                $"SELECT COUNT(*) FROM [{MetricTable}] WHERE [FOrgId] = @org AND [F员工工号] = @no AND [F业务日期] = @d AND [F网点编码] = @code",
                ("@org", OrgId), ("@no", EmpNo), ("@d", batchDay), ("@code", NetCode));

            var result2 = await svc.UnifyShentongAsync(OrgId);
            _log.WriteLine($"[复跑] EmpMetricUpserts={result2.EmployeeMetricUpserts}");

            var countAfter = await CountAsync(conn,
                $"SELECT COUNT(*) FROM [{MetricTable}] WHERE [FOrgId] = @org AND [F员工工号] = @no AND [F业务日期] = @d AND [F网点编码] = @code",
                ("@org", OrgId), ("@no", EmpNo), ("@d", batchDay), ("@code", NetCode));
            _log.WriteLine($"[幂等] 复跑前 {countBefore} → 复跑后 {countAfter}（应相等）");
            Assert.Equal(countBefore, countAfter);
        }
        finally
        {
            await CleanupMetricAsync(conn);
            await CleanupBatchAsync(conn, StgTable, batchId);
            // 种子清理：用列名映射的真实表名（EXP快递业务员名称映射 / EXP业务员 / EXP快递网点）
            if (seededAlias) await ExecAsync(conn, "DELETE FROM [EXP快递业务员名称映射] WHERE [F名称] = @n AND [F组织ID] = @org", ("@n", DirtyName), ("@org", OrgId));
            if (seededSalesman) await ExecAsync(conn, "DELETE FROM [EXP业务员] WHERE [F工号] = @no", ("@no", EmpNo));
            if (seededNetwork) await ExecAsync(conn, "DELETE FROM [EXP快递网点] WHERE [F编号] = @code AND [F组织ID] = @org", ("@code", NetCode), ("@org", OrgId));
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 安排 / 清理 helper
    // ─────────────────────────────────────────────────────────────

    private static async Task<bool> EnsureNetworkAsync(STOTOPDbContext db)
    {
        var exists = await db.Set<ExpNetworkPoint>()
            .IgnoreQueryFilters()
            .AnyAsync(np => np.FCode == NetCode && np.FOrgId == OrgId);
        if (exists) return false;

        db.Set<ExpNetworkPoint>().Add(new ExpNetworkPoint
        {
            FCode = NetCode,
            FFullName = NetFullName,
            FOrgId = OrgId,
            FOwnerOrgId = OrgId,
        });
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<bool> EnsureSalesmanAsync(STOTOPDbContext db)
    {
        var exists = await db.Set<ExpSalesman>()
            .IgnoreQueryFilters()
            .AnyAsync(s => s.FEmployeeNo == EmpNo);
        if (exists) return false;

        db.Set<ExpSalesman>().Add(new ExpSalesman
        {
            FEmployeeNo = EmpNo,
            FNetworkPointCode = NetCode,
            FEmployeeId = 990001,
            FName = "吴健",
            FStatus = 1,
        });
        await db.SaveChangesAsync();
        return true;
    }

    private static async Task<bool> EnsureAliasAsync(STOTOPDbContext db)
    {
        var exists = await db.Set<ExpSalesmanAlias>()
            .IgnoreQueryFilters()
            .AnyAsync(a => a.FName == DirtyName && a.FOrgId == OrgId);
        if (exists) return false;

        db.Set<ExpSalesmanAlias>().Add(new ExpSalesmanAlias
        {
            FName = DirtyName,
            FEmployeeNo = EmpNo,
            FOrgId = OrgId,
        });
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<(long batchId, PluginResult result)> ImportOnceAsync(
        STOTOPDbContext db, string conn, string filePath, long flowDefinitionId, long stageId, long ruleId)
    {
        var localPath = Snapshot(filePath);
        var batch = new CfBatch
        {
            FFlowDefinitionId = flowDefinitionId,
            FOrgId = OrgId,
            FTriggeredById = 1,
            FTriggeredTime = DateTime.Now,
            FTriggerType = "fileUpload",
            FFilePath = localPath,
            FFileName = global::System.IO.Path.GetFileName(filePath),
            FBatchNo = $"TESTC1-{Guid.NewGuid():N}",
            FStatus = CfBatchStatus.Parsing,
            FUploadMethod = "auto",
            FCreatedTime = DateTime.Now,
        };
        db.Set<CfBatch>().Add(batch);
        await db.SaveChangesAsync();
        var batchId = batch.FID;

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
            StageDefinitionId = stageId,
            PluginRuleId = ruleId,
            OrgId = OrgId,
            Services = null!,
        };

        var result = await plugin.ExecuteAsync(ctx);
        return (batchId, result);
    }

    private static async Task CleanupMetricAsync(string conn)
    {
        await ExecAsync(conn, $"DELETE FROM [{MetricTable}] WHERE [FOrgId] = @org", ("@org", OrgId));
    }

    private async Task CleanupBatchAsync(string conn, string table, long batchId)
    {
        if (batchId <= 0) return;
        try { await ExecAsync(conn, $"DELETE FROM [{table}] WHERE [F批次ID] = @b", ("@b", batchId)); }
        catch { /* ignore */ }
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

    private static async Task ExecAsync(string conn, string sql, params (string name, object val)[] ps)
    {
        await using var c = new SqlConnection(conn);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, val) in ps) cmd.Parameters.AddWithValue(name, val);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountAsync(string conn, string sql, params (string name, object val)[] ps)
    {
        await using var c = new SqlConnection(conn);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, val) in ps) cmd.Parameters.AddWithValue(name, val);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<DateTime> ScalarDateAsync(string conn, string sql, params (string name, object val)[] ps)
    {
        await using var c = new SqlConnection(conn);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, val) in ps) cmd.Parameters.AddWithValue(name, val);
        return Convert.ToDateTime(await cmd.ExecuteScalarAsync());
    }
}
