using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Models.Rules;
using STOTOP.Module.CardFlow.Services.Interfaces;

namespace STOTOP.Module.CardFlow.Services;

public sealed class ConditionEvaluationContextBuilder : IConditionEvaluationContextBuilder
{
    private readonly STOTOPDbContext _dbContext;

    public ConditionEvaluationContextBuilder(STOTOPDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConditionEvaluationContext> BuildAsync(
        CfCard card,
        CfStageInstance? currentStage = null,
        CancellationToken cancellationToken = default)
    {
        var details = await _dbContext.Set<CfCardDetail>()
            .Where(detail => detail.FCardId == card.FID)
            .ToListAsync(cancellationToken);

        var context = new ConditionEvaluationContext
        {
            CardData = ParseObject(card.FDataJson),
            SourceContext = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceModule"] = card.FSourceModule,
                ["sourceType"] = card.FSourceType,
                ["sourceId"] = card.FSourceId,
                ["returnUrl"] = card.FReturnUrl,
                ["sourceTitle"] = card.FSourceTitle
            },
            Initiator = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = card.FInitiatorId,
                ["name"] = card.FInitiatorName
            },
            InitiatorOrg = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = card.FOrgId
            },
            CurrentStageResult = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["round"] = card.FCurrentRound,
                ["stageInstanceId"] = currentStage?.FID,
                ["action"] = currentStage?.FFinalAction
            }
        };

        if (currentStage?.FStageDefinitionId != null)
        {
            var stageKey = await _dbContext.Set<CfStageDefinition>()
                .Where(stage => stage.FID == currentStage.FStageDefinitionId.Value)
                .Select(stage => stage.FStageKey)
                .FirstOrDefaultAsync(cancellationToken);
            context.CurrentStageResult["completedStageKey"] = stageKey;
        }

        context.DetailSummary = BuildDetailSummary(details);
        return context;
    }

    private static Dictionary<string, object?> BuildDetailSummary(IEnumerable<CfCardDetail> details)
    {
        var rowCount = 0;
        var amount = 0m;
        var tax = 0m;
        var actualPayAmount = 0m;
        foreach (var detail in details)
        {
            rowCount++;
            var data = ParseObject(detail.FDataJson);
            amount += ReadDecimal(data, "amount");
            tax += ReadDecimal(data, "tax");
            actualPayAmount += ReadDecimal(data, "actualPayAmount")
                + ReadDecimal(data, "payAmount")
                + ReadDecimal(data, "实付金额");
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["rowCount"] = rowCount,
            ["amount"] = amount,
            ["tax"] = tax,
            ["actualPayAmount"] = actualPayAmount == 0m ? amount : actualPayAmount
        };
    }

    private static decimal ReadDecimal(IReadOnlyDictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null)
            return 0m;
        return decimal.TryParse(value.ToString(), out var result) ? result : 0m;
    }

    private static Dictionary<string, object?> ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(
                    property => property.Name,
                    property => ToPlainValue(property.Value),
                    StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static object? ToPlainValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ToPlainValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ToPlainValue(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => element.GetRawText()
        };
    }
}
