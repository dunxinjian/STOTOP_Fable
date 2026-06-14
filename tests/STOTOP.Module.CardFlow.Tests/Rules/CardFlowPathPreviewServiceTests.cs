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
}
