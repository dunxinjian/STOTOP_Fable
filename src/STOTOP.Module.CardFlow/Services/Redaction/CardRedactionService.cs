using System.Text.Json;
using STOTOP.Module.CardFlow.Models.Approval;
using STOTOP.Module.CardFlow.Models.Schema;

namespace STOTOP.Module.CardFlow.Services.Redaction;

public sealed class CardRedactionService : ICardRedactionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CardRedactionResult Redact(CardRedactionRequest request)
    {
        var fields = ReadFields(request.CardSchemaJson);
        var fieldAccess = ComputeFieldAccess(fields, request);

        return new CardRedactionResult
        {
            FieldAccess = fieldAccess,
            DetailAccess = new Dictionary<string, ResolvedAccess>(),
            RedactedDataJson = request.Card.FDataJson ?? "{}", // Task 5 接 allowlist
            RedactedDetails = new List<RedactedDetailRowResult>() // Task 5 接明细
        };
    }

    private static Dictionary<string, ResolvedAccess> ComputeFieldAccess(
        IReadOnlyCollection<CardFieldDefinitionV2> fields,
        CardRedactionRequest request)
    {
        var result = new Dictionary<string, ResolvedAccess>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            var key = field.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (request.ActiveStageConfig != null)
            {
                result[key] = ResolveActive(request.ActiveStageConfig, field);
                continue;
            }

            var rule = new ResolvedAccess
            {
                Access = field.Sensitive ? "masked" : "readonly",
                MaskPattern = field.MaskPattern
            };

            if (Rank(rule.Access) < Rank("readonly") && GrantedBySticky(request.HandledStageConfigs, key))
            {
                rule.Access = "readonly";
            }

            result[key] = rule;
        }

        return result;
    }

    private static ResolvedAccess ResolveActive(StageConfigEnvelope config, CardFieldDefinitionV2 field)
    {
        if (config.ViewProfile?.FieldAccess != null
            && config.ViewProfile.FieldAccess.TryGetValue(field.Key, out var r))
        {
            return new ResolvedAccess
            {
                Access = NormalizeAccess(r.Access),
                MaskPattern = r.MaskPattern ?? field.MaskPattern
            };
        }

        if (config.InputFields.Contains(field.Key))
        {
            return new ResolvedAccess { Access = "editable", MaskPattern = field.MaskPattern };
        }

        return new ResolvedAccess
        {
            Access = field.Sensitive ? "masked" : "readonly",
            MaskPattern = field.MaskPattern
        };
    }

    private static bool GrantedBySticky(
        IReadOnlyCollection<StageConfigEnvelope> handled,
        string key)
    {
        foreach (var config in handled)
        {
            if (config.InputFields.Contains(key))
            {
                return true;
            }

            if (config.ViewProfile?.FieldAccess != null
                && config.ViewProfile.FieldAccess.TryGetValue(key, out var r)
                && Rank(NormalizeAccess(r.Access)) >= Rank("readonly"))
            {
                return true;
            }
        }

        return false;
    }

    private static int Rank(string access) => access switch
    {
        "hidden" => 0,
        "masked" => 1,
        "readonly" => 2,
        "editable" => 3,
        "required" => 3,
        _ => 2
    };

    private static string NormalizeAccess(string? access) => access?.Trim().ToLowerInvariant() switch
    {
        "hidden" => "hidden",
        "masked" => "masked",
        "editable" => "editable",
        "required" => "editable",
        _ => "readonly"
    };

    private static List<CardFieldDefinitionV2> ReadFields(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            return new List<CardFieldDefinitionV2>();
        }

        try
        {
            using var document = JsonDocument.Parse(schemaJson);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<CardFieldDefinitionV2>>(schemaJson, JsonOptions) ?? new();
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                var schema = JsonSerializer.Deserialize<CardSchemaV2>(schemaJson, JsonOptions);
                return schema?.Fields ?? new List<CardFieldDefinitionV2>();
            }
        }
        catch (JsonException)
        {
            // ignore，返回空
        }

        return new List<CardFieldDefinitionV2>();
    }
}
