using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using STOTOP.Core.Models;
using STOTOP.Module.CardFlow.AutoPlugin;
using STOTOP.Module.CardFlow.Dtos;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Models.Approval;
using STOTOP.Module.CardFlow.Services;
using STOTOP.Module.CardFlow.Services.Interfaces;
using STOTOP.Module.Finance.Dtos;
using STOTOP.Module.Finance.Services.Interfaces;
using STOTOP.Module.System.Entities;
using Xunit;

namespace STOTOP.Module.CardFlow.Tests.Approval;

public class ExpenseApprovalE2ETests
{
    [Fact]
    public async global::System.Threading.Tasks.Task ExpenseFlow_DeptThenFinance_Approve_Completes()
    {
        using var db = TestDbContextFactory.Create(nameof(ExpenseFlow_DeptThenFinance_Approve_Completes));

        db.Set<SysUser>().AddRange(
            new SysUser { FID = 21, FName = "部门经理" },
            new SysUser { FID = 31, FName = "财务" });

        db.Set<CfFlowVersion>().Add(new CfFlowVersion
        {
            FID = 2263, FFlowDefinitionId = 2262, FStatus = "published", FIsCurrentVersion = true
        });
        db.Set<CfStageDefinition>().AddRange(
            new CfStageDefinition
            {
                FID = 5031, FFlowVersionId = 2263, FSortOrder = 1, FStageName = "部门负责人审批",
                FType = "human", FApprovalMode = "single", FAssigneeStrategy = "fixedUsers",
                FAssigneeConfigJson = """{"users":[{"userId":21,"userName":"部门经理"}]}"""
            },
            new CfStageDefinition
            {
                FID = 5032, FFlowVersionId = 2263, FSortOrder = 2, FStageName = "财务审批",
                FType = "human", FApprovalMode = "single", FAssigneeStrategy = "fixedUsers",
                FAssigneeConfigJson = """{"users":[{"userId":31,"userName":"财务"}]}"""
            });

        var card = new CfCard
        {
            FID = 9100, FFlowDefinitionId = 2262, FFlowVersionId = 2263,
            FTitle = "费用报销", FStatus = "active", FInitiatorId = 99, FInitiatorName = "发起人",
            FCurrentStageInstanceId = 9201, FCurrentRound = 1, FOrgId = 1, FDataJson = "{}"
        };
        db.Set<CfStageInstance>().Add(new CfStageInstance
        {
            FID = 9201, FCardId = 9100, FStageDefinitionId = 5031, FStageName = "部门负责人审批",
            FType = "human", FApprovalMode = "single", FRound = 1, FStatus = "active"
        });
        db.Set<CfStageAssignee>().Add(new CfStageAssignee
        {
            FID = 9301, FStageInstanceId = 9201, FUserId = 21, FUserName = "部门经理",
            FStatus = "pending", FAssignedTime = DateTime.Now
        });
        db.Set<CfTodoItem>().Add(new CfTodoItem
        {
            FID = 9401, FCardId = 9100, FStageInstanceId = 9201, FHandlerId = 21,
            FHandlerName = "部门经理", FStatus = "pending", FOrgId = 1
        });
        db.Set<CfCard>().Add(card);
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);

        await engine.ApproveAsync(9100, 21, new ApproveRequest { Opinion = "同意" });
        var afterDept = await db.Set<CfCard>().FindAsync(9100L);
        Assert.Equal("active", afterDept!.FStatus);
        var financeStage = db.Set<CfStageInstance>().Single(s => s.FCardId == 9100 && s.FStageDefinitionId == 5032);
        Assert.Equal("active", financeStage.FStatus);

        await engine.ApproveAsync(9100, 31, new ApproveRequest { Opinion = "同意" });
        var done = await db.Set<CfCard>().FindAsync(9100L);
        Assert.Equal("completed", done!.FStatus);
    }

    [Fact]
    public async global::System.Threading.Tasks.Task ExpenseFlow_Submit_FromDraft_ActivatesFirstStage()
    {
        using var db = TestDbContextFactory.Create(nameof(ExpenseFlow_Submit_FromDraft_ActivatesFirstStage));

        db.Set<SysUser>().Add(new SysUser { FID = 21, FName = "部门经理" });
        // SubmitAsync 需要查询 CfFlowDefinition（步骤2）
        db.Set<CfFlowDefinition>().Add(new CfFlowDefinition
        {
            FID = 2262, FFlowName = "费用报销", FFlowCode = "expense", FOrgId = 1,
            FStatus = "published", FCreatorId = 1, FCreatedTime = DateTime.Now
        });
        db.Set<CfFlowVersion>().Add(new CfFlowVersion
        {
            FID = 2263, FFlowDefinitionId = 2262, FStatus = "published", FIsCurrentVersion = true
        });
        db.Set<CfStageDefinition>().Add(new CfStageDefinition
        {
            FID = 5031, FFlowVersionId = 2263, FSortOrder = 1, FStageName = "部门负责人审批",
            FType = "human", FApprovalMode = "single", FAssigneeStrategy = "fixedUsers",
            FAssigneeConfigJson = """{"users":[{"userId":21,"userName":"部门经理"}]}"""
        });
        db.Set<CfCard>().Add(new CfCard
        {
            FID = 9500, FFlowDefinitionId = 2262, FFlowVersionId = 2263,
            FTitle = "费用报销", FStatus = "draft", FInitiatorId = 99, FInitiatorName = "发起人",
            FCurrentRound = 1, FOrgId = 1, FDataJson = "{}"
        });
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);
        await engine.SubmitAsync(9500, 99);

        var card = await db.Set<CfCard>().FindAsync(9500L);
        Assert.Equal("active", card!.FStatus);
        var firstStage = db.Set<CfStageInstance>().Single(s => s.FCardId == 9500 && s.FStageDefinitionId == 5031);
        Assert.Equal("active", firstStage.FStatus);
    }

    private static FlowEngineService CreateEngine(STOTOP.Infrastructure.Data.STOTOPDbContext db)
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var orchestration = new OrchestrationEngineService(db, NullLogger<OrchestrationEngineService>.Instance);

        return new FlowEngineService(
            db,
            new FakeNumberSequenceService(),
            new FakeCardSchemaService(),
            new ApprovalModeHandler(),
            new SequentialApprovalRuntime(),
            new ReturnToStageRuntime(),
            new StageConfigParser(),
            new StageFieldAccessService(),
            new StageActionPolicyService(),
            new ConditionEvaluator(),
            new ApproverResolver(db),
            new FakeBudgetOccupationService(),
            new DbTodoService(db),
            new FakeNotificationDispatcher(),
            new AutoPluginFactory(provider),
            provider,
            provider.GetRequiredService<IServiceScopeFactory>(),
            orchestration,
            new FakeBatchNotifier(),
            new FakeBatchLifecycleService(),
            NullLogger<FlowEngineService>.Instance);
    }
}
