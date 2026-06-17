using System.Text.Json;
using System.Text.Json.Nodes;
using STOTOP.Module.CardFlow.Entities;
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

        var detailFields = ReadFields(request.DetailSchemaJson);
        var detailAccess = ComputeDetailAccess(detailFields, request);

        return new CardRedactionResult
        {
            FieldAccess = fieldAccess,
            DetailAccess = detailAccess,
            RedactedDataJson = ApplyAllowlist(request.Card.FDataJson, fieldAccess),
            RedactedDetails = request.Details
                .OrderBy(d => d.FSortOrder)
                .Select(d => new RedactedDetailRowResult
                {
                    Id = d.FID,
                    SortOrder = d.FSortOrder,
                    DetailTableKey = NormalizeTable(d.FDetailTableKey),
                    DataJson = ApplyDetailAllowlist(d.FDataJson, NormalizeTable(d.FDetailTableKey), detailAccess)
                })
                .ToList()
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

            // 设计：查看者正作为当前 active 节点处理人时，该节点视图对其权威（可提权，也可限制到 hidden/masked）。
            // 粘附授权（D4）在其"非 active"时生效——卡片流转走后再查看，下方 baseline+sticky 分支会恢复其历史获授的明文。
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
        _ => 1
    };

    private static string NormalizeAccess(string? access) => access?.Trim().ToLowerInvariant() switch
    {
        "hidden" => "hidden",
        "masked" => "masked",
        "editable" => "editable",
        "required" => "editable",
        "readonly" => "readonly",
        // 未知/拼错/null/空 一律按最严处理（fail-closed），不得默认成明文可见
        _ => "masked"
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

    private static Dictionary<string, ResolvedAccess> ComputeDetailAccess(
        IReadOnlyCollection<CardFieldDefinitionV2> detailFields,
        CardRedactionRequest request)
    {
        // 明细列基线（按列敏感位）；明细 MVP 与字段同基线规则，仅叠加 active 节点 DetailAccess 覆盖。
        var result = new Dictionary<string, ResolvedAccess>(StringComparer.Ordinal);
        foreach (var col in detailFields)
        {
            if (string.IsNullOrWhiteSpace(col.Key))
            {
                continue;
            }

            var access = new ResolvedAccess
            {
                Access = col.Sensitive ? "masked" : "readonly",
                MaskPattern = col.MaskPattern
            };

            if (request.ActiveStageConfig?.ViewProfile?.DetailAccess != null
                && request.ActiveStageConfig.ViewProfile.DetailAccess.TryGetValue($"default.{col.Key}", out var r))
            {
                access.Access = NormalizeAccess(r.Access);
                access.MaskPattern = r.MaskPattern ?? col.MaskPattern;
            }

            result[$"default.{col.Key}"] = access;
        }

        return result;
    }

    private static string ApplyAllowlist(string? dataJson, Dictionary<string, ResolvedAccess> access)
    {
        var obj = ParseObject(dataJson);
        var output = new JsonObject();
        foreach (var (key, rule) in access)
        {
            if (rule.Access == "hidden" || !obj.TryGetPropertyValue(key, out var val))
            {
                continue;
            }

            output[key] = rule.Access == "masked"
                ? FieldMasker.Mask(NodeToText(val), rule.MaskPattern)
                : val?.DeepClone();
        }

        return output.ToJsonString();
    }

    private static string ApplyDetailAllowlist(
        string? dataJson, string tableKey, Dictionary<string, ResolvedAccess> detailAccess)
    {
        var obj = ParseObject(dataJson);
        var output = new JsonObject();
        var prefix = $"{tableKey}.";
        foreach (var (accessKey, rule) in detailAccess)
        {
            if (!accessKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var col = accessKey[prefix.Length..];
            if (rule.Access == "hidden" || !obj.TryGetPropertyValue(col, out var val))
            {
                continue;
            }

            output[col] = rule.Access == "masked"
                ? FieldMasker.Mask(NodeToText(val), rule.MaskPattern)
                : val?.DeepClone();
        }

        return output.ToJsonString();
    }

    private static string NormalizeTable(string? key)
        => string.IsNullOrWhiteSpace(key) ? "default" : key!;

    private static string NodeToText(JsonNode? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value.GetValueKind() == JsonValueKind.String
            ? value.GetValue<string>()
            : value.ToJsonString();
    }

    private static JsonObject ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }
}
