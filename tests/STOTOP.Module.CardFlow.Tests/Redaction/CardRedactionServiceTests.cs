using STOTOP.Module.CardFlow.Entities;
using STOTOP.Module.CardFlow.Models.Approval;
using STOTOP.Module.CardFlow.Models.Schema;
using STOTOP.Module.CardFlow.Services.Redaction;
using Xunit;

namespace STOTOP.Module.CardFlow.Tests.Redaction;

public class CardRedactionServiceTests
{
    private const string Schema =
        """[{"key":"amount","type":"money"},{"key":"payeeAccountNo","type":"text","sensitive":true,"maskPattern":"bankCard"}]""";

    private static CfCard Card() => new()
    {
        FDataJson = """{"amount":88,"payeeAccountNo":"6222021234567890123","legacyNote":"x"}"""
    };

    [Fact]
    public void NoActiveNoGrant_SensitiveMasked_NonSensitiveReadonly()
    {
        var svc = new CardRedactionService();
        var result = svc.Redact(new CardRedactionRequest { Card = Card(), CardSchemaJson = Schema });

        Assert.Equal("readonly", result.FieldAccess["amount"].Access);
        Assert.Equal("masked", result.FieldAccess["payeeAccountNo"].Access);
        Assert.Equal("bankCard", result.FieldAccess["payeeAccountNo"].MaskPattern);
    }

    [Fact]
    public void StickyGrant_PastHandlerKeepsClearView()
    {
        var svc = new CardRedactionService();
        var handled = new StageConfigEnvelope
        {
            ViewProfile = new StageViewProfile
            {
                FieldAccess = new() { ["payeeAccountNo"] = new() { Access = "readonly" } }
            }
        };
        var result = svc.Redact(new CardRedactionRequest
        {
            Card = Card(),
            CardSchemaJson = Schema,
            HandledStageConfigs = new[] { handled }
        });

        Assert.Equal("readonly", result.FieldAccess["payeeAccountNo"].Access);
    }

    [Fact]
    public void ActiveAssignee_NodeConfigIsAuthoritative()
    {
        var svc = new CardRedactionService();
        var active = new StageConfigEnvelope
        {
            Version = 2,
            InputFields = new() { "amount" },
            ViewProfile = new StageViewProfile
            {
                FieldAccess = new() { ["payeeAccountNo"] = new() { Access = "editable" } }
            }
        };
        var result = svc.Redact(new CardRedactionRequest
        {
            Card = Card(),
            CardSchemaJson = Schema,
            ActiveStageConfig = active
        });

        Assert.Equal("editable", result.FieldAccess["amount"].Access);
        Assert.Equal("editable", result.FieldAccess["payeeAccountNo"].Access);
    }

    [Fact]
    public void ActiveAssignee_HiddenOverridesBaseline()
    {
        var svc = new CardRedactionService();
        var active = new StageConfigEnvelope
        {
            Version = 2,
            ViewProfile = new StageViewProfile
            {
                FieldAccess = new() { ["amount"] = new() { Access = "hidden" } }
            }
        };
        var result = svc.Redact(new CardRedactionRequest
        {
            Card = Card(),
            CardSchemaJson = Schema,
            ActiveStageConfig = active
        });

        Assert.Equal("hidden", result.FieldAccess["amount"].Access);
        Assert.Equal("masked", result.FieldAccess["payeeAccountNo"].Access);
    }

    [Fact]
    public void UnknownAccessString_OnActiveNode_FailsClosedToMasked()
    {
        var svc = new CardRedactionService();
        var active = new StageConfigEnvelope
        {
            Version = 2,
            ViewProfile = new StageViewProfile
            {
                // 配置笔误/枚举漂移：未知 access 串绝不能变明文
                FieldAccess = new() { ["payeeAccountNo"] = new() { Access = "redaonly" } }
            }
        };
        var result = svc.Redact(new CardRedactionRequest { Card = Card(), CardSchemaJson = Schema, ActiveStageConfig = active });
        Assert.Equal("masked", result.FieldAccess["payeeAccountNo"].Access);
    }

    [Fact]
    public void UnknownAccessString_OnHandledNode_DoesNotGrantStickyClearView()
    {
        var svc = new CardRedactionService();
        var handled = new StageConfigEnvelope
        {
            ViewProfile = new StageViewProfile
            {
                FieldAccess = new() { ["payeeAccountNo"] = new() { Access = "??" } }
            }
        };
        var result = svc.Redact(new CardRedactionRequest { Card = Card(), CardSchemaJson = Schema, HandledStageConfigs = new[] { handled } });
        // 未知串不构成提权 → 敏感字段保持 masked
        Assert.Equal("masked", result.FieldAccess["payeeAccountNo"].Access);
    }

    [Fact]
    public void StickyGrant_ViaInputFields_LiftsSensitiveToReadonly()
    {
        var svc = new CardRedactionService();
        var handled = new StageConfigEnvelope { InputFields = new() { "payeeAccountNo" } };
        var result = svc.Redact(new CardRedactionRequest { Card = Card(), CardSchemaJson = Schema, HandledStageConfigs = new[] { handled } });
        Assert.Equal("readonly", result.FieldAccess["payeeAccountNo"].Access);
    }

    [Fact]
    public void NullSchema_ProducesEmptyFieldAccess()
    {
        var svc = new CardRedactionService();
        var result = svc.Redact(new CardRedactionRequest { Card = Card(), CardSchemaJson = null });
        Assert.Empty(result.FieldAccess);
    }
}
