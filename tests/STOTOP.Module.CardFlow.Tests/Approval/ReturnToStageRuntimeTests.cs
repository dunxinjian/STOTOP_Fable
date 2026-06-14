using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Services;
using Xunit;

namespace STOTOP.Module.CardFlow.Tests.Approval;

public class ReturnToStageRuntimeTests
{
    [Fact]
    public void ResolvePreviousTarget_ChoosesNearestCompletedHumanStageInCurrentRound()
    {
        var runtime = new ReturnToStageRuntime();
        var stages = new List<CfStageDefinition>
        {
            new() { FID = 10, FSortOrder = 1, FType = "human" },
            new() { FID = 20, FSortOrder = 2, FType = "auto" },
            new() { FID = 30, FSortOrder = 3, FType = "human" },
            new() { FID = 40, FSortOrder = 4, FType = "human" }
        };
        var instances = new List<CfStageInstance>
        {
            new() { FID = 101, FStageDefinitionId = 10, FType = "human", FStatus = "completed", FRound = 2 },
            new() { FID = 102, FStageDefinitionId = 20, FType = "auto", FStatus = "completed", FRound = 2 },
            new() { FID = 103, FStageDefinitionId = 30, FType = "human", FStatus = "completed", FRound = 2 },
            new() { FID = 104, FStageDefinitionId = 30, FType = "human", FStatus = "completed", FRound = 1 }
        };

        var result = runtime.ResolvePreviousTarget(stages, instances, currentStageDefinitionId: 40, currentRound: 2);

        Assert.True(result.Success);
        Assert.Equal(30, result.TargetStageDefinition!.FID);
        Assert.Equal(103, result.VisitedStageInstance!.FID);
    }

    [Fact]
    public void ResolveSpecifiedTarget_RejectsUnvisitedOrLaterTargets()
    {
        var runtime = new ReturnToStageRuntime();
        var stages = new List<CfStageDefinition>
        {
            new() { FID = 10, FSortOrder = 1, FType = "human" },
            new() { FID = 20, FSortOrder = 2, FType = "human" },
            new() { FID = 30, FSortOrder = 3, FType = "human" }
        };
        var instances = new List<CfStageInstance>
        {
            new() { FID = 101, FStageDefinitionId = 10, FType = "human", FStatus = "completed", FRound = 1 }
        };

        var unvisited = runtime.ResolveSpecifiedTarget(stages, instances, currentStageDefinitionId: 30, currentRound: 1, targetStageDefinitionId: 20);
        var later = runtime.ResolveSpecifiedTarget(stages, instances, currentStageDefinitionId: 20, currentRound: 1, targetStageDefinitionId: 30);

        Assert.False(unvisited.Success);
        Assert.Contains("未在当前轮次完成", unvisited.ErrorMessage);
        Assert.False(later.Success);
        Assert.Contains("当前节点之前", later.ErrorMessage);
    }

    [Fact]
    public void SupersedeDownstreamCompletedStages_CancelsCompletedHumanStagesAfterTarget()
    {
        var runtime = new ReturnToStageRuntime();
        var stages = new List<CfStageDefinition>
        {
            new() { FID = 10, FSortOrder = 1, FType = "human" },
            new() { FID = 20, FSortOrder = 2, FType = "human" },
            new() { FID = 30, FSortOrder = 3, FType = "auto" },
            new() { FID = 40, FSortOrder = 4, FType = "human" }
        };
        var instances = new List<CfStageInstance>
        {
            new() { FID = 101, FStageDefinitionId = 10, FType = "human", FStatus = "completed", FRound = 1 },
            new() { FID = 102, FStageDefinitionId = 20, FType = "human", FStatus = "completed", FRound = 1 },
            new() { FID = 103, FStageDefinitionId = 30, FType = "auto", FStatus = "completed", FRound = 1 },
            new() { FID = 104, FStageDefinitionId = 40, FType = "human", FStatus = "active", FRound = 1 }
        };

        var superseded = runtime.SupersedeDownstreamCompletedStages(stages, instances, targetStageDefinitionId: 10, currentRound: 1);

        Assert.Equal(new long[] { 102 }, superseded.Select(s => s.FID));
        Assert.Equal("cancelled", instances.Single(i => i.FID == 102).FStatus);
        Assert.Equal("return-superseded", instances.Single(i => i.FID == 102).FFinalAction);
        Assert.Equal("completed", instances.Single(i => i.FID == 103).FStatus);
    }
}
