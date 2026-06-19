using STOTOP.Module.CardFlow.Dtos;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Services;
using Xunit;

namespace STOTOP.Module.CardFlow.Tests.Rules;

public class CardFlowPathPreviewServiceTests
{
    [Fact]
    public async global::System.Threading.Tasks.Task PreviewDraftVersion_FollowsConditionalRouteAndDefaultBranch()
    {
        using var db = TestDbContextFactory.Create(nameof(PreviewDraftVersion_FollowsConditionalRouteAndDefaultBranch));
        SeedFlow(db);
        await db.SaveChangesAsync();

        var service = new CardFlowPathPreviewService(
            db,
            new ConditionRuleEvaluator(),
            new AuditSnapshotPolicyService());

        var large = await service.PreviewDraftVersionAsync(100, new CardFlowPathPreviewRequest
        {
            DataJson = """{"amount":6800}"""
        });
        var small = await service.PreviewDraftVersionAsync(100, new CardFlowPathPreviewRequest
        {
            DataJson = """{"amount":1200}"""
        });

        Assert.Equal(new[] { "manager", "gm", "finance" }, large.Steps.Select(step => step.StageKey));
        Assert.Equal("large_amount", large.Steps[0].SelectedEdgeKey);
        Assert.Equal(new[] { "manager", "finance" }, small.Steps.Select(step => step.StageKey));
        Assert.Equal("default_finance", small.Steps[0].SelectedEdgeKey);
    }

    private static void SeedFlow(STOTOP.Infrastructure.Data.STOTOPDbContext db)
    {
        db.Set<CfFlowDefinition>().Add(new CfFlowDefinition
        {
            FID = 100,
            FFlowName = "费用报销",
            FFlowCode = "FYBS",
            FStatus = "draft",
            FOrgId = 1,
            FCreatedTime = DateTime.Now
        });
        db.Set<CfFlowVersion>().Add(new CfFlowVersion
        {
            FID = 200,
            FFlowDefinitionId = 100,
            FVersionNumber = 1,
            FStatus = "draft",
            FCreatedTime = DateTime.Now
        });
        db.Set<CfStageDefinition>().AddRange(
            new CfStageDefinition { FID = 1, FFlowVersionId = 200, FStageKey = "manager", FStageName = "主管审批", FType = "human", FSortOrder = 1 },
            new CfStageDefinition { FID = 2, FFlowVersionId = 200, FStageKey = "finance", FStageName = "财务复核", FType = "human", FSortOrder = 2 },
            new CfStageDefinition { FID = 3, FFlowVersionId = 200, FStageKey = "gm", FStageName = "总经理审批", FType = "human", FSortOrder = 3 });
        db.Set<CfStageRouteRule>().AddRange(
            new CfStageRouteRule
            {
                FFlowVersionId = 200,
                FEdgeKey = "large_amount",
                FFromStageKey = "manager",
                FToStageKey = "gm",
                FRouteName = "大额报销",
                FConditionJson = """{"field":"card.amount","operator":"gte","value":5000}""",
                FPriority = 1,
                FStatus = "active"
            },
            new CfStageRouteRule
            {
                FFlowVersionId = 200,
                FEdgeKey = "default_finance",
                FFromStageKey = "manager",
                FToStageKey = "finance",
                FRouteName = "其他情况",
                FPriority = 99,
                FIsDefault = true,
                FStatus = "active"
            },
            new CfStageRouteRule
            {
                FFlowVersionId = 200,
                FEdgeKey = "gm_to_finance",
                FFromStageKey = "gm",
                FToStageKey = "finance",
                FRouteName = "总经理后财务",
                FPriority = 99,
                FIsDefault = true,
                FStatus = "active"
            });
    }

    [Fact]
    public async global::System.Threading.Tasks.Task PreviewDraftVersion_OrgRouting_UsesInitiatorOrgId()
    {
        using var db = TestDbContextFactory.Create(nameof(PreviewDraftVersion_OrgRouting_UsesInitiatorOrgId));
        SeedOrgRoutingFlow(db);
        await db.SaveChangesAsync();

        var service = new CardFlowPathPreviewService(db, new ConditionRuleEvaluator(), new AuditSnapshotPolicyService());

        var hit = await service.PreviewDraftVersionAsync(300, new CardFlowPathPreviewRequest
        {
            DataJson = "{}",
            OrgId = 9
        });

        // InitiatorOrg 键收敛为 id 后，initiatorOrg.id == 9 命中 vip 分支
        Assert.Equal("vip_org", hit.Steps[0].SelectedEdgeKey);
        Assert.Equal("vip", hit.Steps[1].StageKey);
    }

    [Fact]
    public async global::System.Threading.Tasks.Task PreviewDraftVersion_BuildsDetailSummaryWithZeroRowCount()
    {
        using var db = TestDbContextFactory.Create(nameof(PreviewDraftVersion_BuildsDetailSummaryWithZeroRowCount));
        SeedDetailSummaryFlow(db);
        await db.SaveChangesAsync();

        var service = new CardFlowPathPreviewService(db, new ConditionRuleEvaluator(), new AuditSnapshotPolicyService());

        var result = await service.PreviewDraftVersionAsync(310, new CardFlowPathPreviewRequest
        {
            DataJson = "{}"
        });

        // 预演无明细 → DetailSummary 补建为 rowCount=0；detailSummary.rowCount eq 0 命中
        Assert.Equal("no_detail", result.Steps[0].SelectedEdgeKey);
    }

    private static void SeedOrgRoutingFlow(STOTOP.Infrastructure.Data.STOTOPDbContext db)
    {
        db.Set<CfFlowDefinition>().Add(new CfFlowDefinition { FID = 300, FFlowName = "组织路由", FFlowCode = "ORG", FStatus = "draft", FOrgId = 1, FCreatedTime = DateTime.Now });
        db.Set<CfFlowVersion>().Add(new CfFlowVersion { FID = 301, FFlowDefinitionId = 300, FVersionNumber = 1, FStatus = "draft", FCreatedTime = DateTime.Now });
        db.Set<CfStageDefinition>().AddRange(
            new CfStageDefinition { FID = 10, FFlowVersionId = 301, FStageKey = "start", FStageName = "开始", FType = "human", FSortOrder = 1 },
            new CfStageDefinition { FID = 11, FFlowVersionId = 301, FStageKey = "vip", FStageName = "VIP", FType = "human", FSortOrder = 2 },
            new CfStageDefinition { FID = 12, FFlowVersionId = 301, FStageKey = "normal", FStageName = "普通", FType = "human", FSortOrder = 3 });
        db.Set<CfStageRouteRule>().AddRange(
            new CfStageRouteRule { FFlowVersionId = 301, FEdgeKey = "vip_org", FFromStageKey = "start", FToStageKey = "vip", FRouteName = "VIP组织", FConditionJson = """{"field":"initiatorOrg.id","operator":"eq","value":9}""", FPriority = 1, FStatus = "active" },
            new CfStageRouteRule { FFlowVersionId = 301, FEdgeKey = "normal_default", FFromStageKey = "start", FToStageKey = "normal", FRouteName = "其他", FPriority = 99, FIsDefault = true, FStatus = "active" });
        // vip / normal 无出边(终端)，预演到达即止，路径稳定
    }

    private static void SeedDetailSummaryFlow(STOTOP.Infrastructure.Data.STOTOPDbContext db)
    {
        db.Set<CfFlowDefinition>().Add(new CfFlowDefinition { FID = 310, FFlowName = "明细汇总", FFlowCode = "DS", FStatus = "draft", FOrgId = 1, FCreatedTime = DateTime.Now });
        db.Set<CfFlowVersion>().Add(new CfFlowVersion { FID = 311, FFlowDefinitionId = 310, FVersionNumber = 1, FStatus = "draft", FCreatedTime = DateTime.Now });
        db.Set<CfStageDefinition>().AddRange(
            new CfStageDefinition { FID = 20, FFlowVersionId = 311, FStageKey = "begin", FStageName = "起", FType = "human", FSortOrder = 1 },
            new CfStageDefinition { FID = 21, FFlowVersionId = 311, FStageKey = "empty", FStageName = "无明细", FType = "human", FSortOrder = 2 },
            new CfStageDefinition { FID = 22, FFlowVersionId = 311, FStageKey = "other", FStageName = "其他", FType = "human", FSortOrder = 3 });
        db.Set<CfStageRouteRule>().AddRange(
            new CfStageRouteRule { FFlowVersionId = 311, FEdgeKey = "no_detail", FFromStageKey = "begin", FToStageKey = "empty", FRouteName = "无明细", FConditionJson = """{"field":"detailSummary.rowCount","operator":"eq","value":0}""", FPriority = 1, FStatus = "active" },
            new CfStageRouteRule { FFlowVersionId = 311, FEdgeKey = "ds_default", FFromStageKey = "begin", FToStageKey = "other", FRouteName = "其他", FPriority = 99, FIsDefault = true, FStatus = "active" });
        // empty / other 终端
    }
}
