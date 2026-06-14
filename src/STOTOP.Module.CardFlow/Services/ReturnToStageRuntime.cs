using STOTOP.Module.CardFlow.Entities;

namespace STOTOP.Module.CardFlow.Services;

public sealed class ReturnToStageRuntime
{
    public ReturnToStageTargetResult ResolvePreviousTarget(
        IReadOnlyCollection<CfStageDefinition> stages,
        IReadOnlyCollection<CfStageInstance> stageInstances,
        long? currentStageDefinitionId,
        int currentRound)
    {
        var currentDefinition = FindStage(stages, currentStageDefinitionId);
        if (currentDefinition == null)
            return ReturnToStageTargetResult.Fail("当前节点定义不存在");

        var stageById = stages.ToDictionary(s => s.FID);
        var candidate = stageInstances
            .Where(instance =>
                instance.FRound == currentRound &&
                instance.FStatus == "completed" &&
                !instance.FIsDynamicInsert &&
                instance.FStageDefinitionId.HasValue &&
                stageById.TryGetValue(instance.FStageDefinitionId.Value, out var definition) &&
                IsHuman(definition) &&
                definition.FSortOrder < currentDefinition.FSortOrder)
            .Select(instance => new
            {
                Instance = instance,
                Definition = stageById[instance.FStageDefinitionId!.Value]
            })
            .OrderByDescending(x => x.Definition.FSortOrder)
            .ThenByDescending(x => x.Instance.FID)
            .FirstOrDefault();

        return candidate == null
            ? ReturnToStageTargetResult.Fail("未找到可退回的上一人工节点")
            : ReturnToStageTargetResult.Ok(candidate.Definition, candidate.Instance);
    }

    public ReturnToStageTargetResult ResolveSpecifiedTarget(
        IReadOnlyCollection<CfStageDefinition> stages,
        IReadOnlyCollection<CfStageInstance> stageInstances,
        long? currentStageDefinitionId,
        int currentRound,
        long? targetStageDefinitionId)
    {
        if (!targetStageDefinitionId.HasValue)
            return ReturnToStageTargetResult.Fail("未指定退回目标节点");

        var currentDefinition = FindStage(stages, currentStageDefinitionId);
        if (currentDefinition == null)
            return ReturnToStageTargetResult.Fail("当前节点定义不存在");

        var targetDefinition = FindStage(stages, targetStageDefinitionId);
        if (targetDefinition == null)
            return ReturnToStageTargetResult.Fail("退回目标节点不属于当前流程版本");
        if (!IsHuman(targetDefinition))
            return ReturnToStageTargetResult.Fail("退回目标节点不是人工节点");
        if (targetDefinition.FSortOrder >= currentDefinition.FSortOrder)
            return ReturnToStageTargetResult.Fail("退回目标节点必须在当前节点之前");

        var visitedInstance = stageInstances
            .Where(instance =>
                instance.FRound == currentRound &&
                instance.FStatus == "completed" &&
                !instance.FIsDynamicInsert &&
                instance.FStageDefinitionId == targetDefinition.FID)
            .OrderByDescending(instance => instance.FID)
            .FirstOrDefault();

        return visitedInstance == null
            ? ReturnToStageTargetResult.Fail("退回目标节点未在当前轮次完成")
            : ReturnToStageTargetResult.Ok(targetDefinition, visitedInstance);
    }

    public IReadOnlyList<CfStageInstance> SupersedeDownstreamCompletedStages(
        IReadOnlyCollection<CfStageDefinition> stages,
        IReadOnlyCollection<CfStageInstance> stageInstances,
        long targetStageDefinitionId,
        int currentRound)
    {
        var targetDefinition = FindStage(stages, targetStageDefinitionId);
        if (targetDefinition == null)
            return Array.Empty<CfStageInstance>();

        var stageById = stages.ToDictionary(s => s.FID);
        var superseded = stageInstances
            .Where(instance =>
                instance.FRound == currentRound &&
                instance.FStatus == "completed" &&
                !instance.FIsDynamicInsert &&
                instance.FStageDefinitionId.HasValue &&
                stageById.TryGetValue(instance.FStageDefinitionId.Value, out var definition) &&
                IsHuman(definition) &&
                definition.FSortOrder > targetDefinition.FSortOrder)
            .ToList();

        foreach (var instance in superseded)
        {
            instance.FStatus = "cancelled";
            instance.FFinalAction = "return-superseded";
            instance.FCompletedTime = DateTime.Now;
        }

        return superseded;
    }

    private static CfStageDefinition? FindStage(IEnumerable<CfStageDefinition> stages, long? id)
        => id.HasValue ? stages.FirstOrDefault(stage => stage.FID == id.Value) : null;

    private static bool IsHuman(CfStageDefinition stage)
        => string.Equals(stage.FType, "human", StringComparison.OrdinalIgnoreCase);
}

public sealed class ReturnToStageTargetResult
{
    public bool Success => string.IsNullOrWhiteSpace(ErrorMessage);
    public string? ErrorMessage { get; init; }
    public CfStageDefinition? TargetStageDefinition { get; init; }
    public CfStageInstance? VisitedStageInstance { get; init; }

    public static ReturnToStageTargetResult Ok(CfStageDefinition definition, CfStageInstance instance) => new()
    {
        TargetStageDefinition = definition,
        VisitedStageInstance = instance
    };

    public static ReturnToStageTargetResult Fail(string message) => new()
    {
        ErrorMessage = message
    };
}
