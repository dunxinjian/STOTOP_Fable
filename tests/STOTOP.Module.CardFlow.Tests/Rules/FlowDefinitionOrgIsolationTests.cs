using Microsoft.EntityFrameworkCore;
using STOTOP.Module.CardFlow.Entities;
using Xunit;

namespace STOTOP.Module.CardFlow.Tests.Rules;

public class FlowDefinitionOrgIsolationTests
{
    [Fact]
    public void SuppressOrgIdFill_KeepsOrgZero_WhileNormalAddGetsCurrentOrg()
    {
        using var db = TestDbContextFactory.Create(nameof(SuppressOrgIdFill_KeepsOrgZero_WhileNormalAddGetsCurrentOrg), 192);

        using (db.SuppressOrgIdFill())
        {
            db.Set<CfFlowDefinition>().Add(new CfFlowDefinition { FFlowName = "T", FFlowCode = "TPL_A", FOrgId = 0, FStatus = "published" });
            db.SaveChanges();
        }
        var tpl = db.Set<CfFlowDefinition>().IgnoreQueryFilters().Single(x => x.FFlowCode == "TPL_A");
        Assert.Equal(0, tpl.FOrgId);

        db.Set<CfFlowDefinition>().Add(new CfFlowDefinition { FFlowName = "N", FFlowCode = "NORMAL_A", FOrgId = 0, FStatus = "draft" });
        db.SaveChanges();
        var normal = db.Set<CfFlowDefinition>().IgnoreQueryFilters().Single(x => x.FFlowCode == "NORMAL_A");
        Assert.Equal(192, normal.FOrgId);
    }

    [Fact]
    public async global::System.Threading.Tasks.Task GetTemplatesAsync_ReturnsOnlyGlobalTemplates()
    {
        using var db = TestDbContextFactory.Create(nameof(GetTemplatesAsync_ReturnsOnlyGlobalTemplates), null);
        db.Set<CfFlowDefinition>().AddRange(
            new CfFlowDefinition { FFlowName = "全局模板", FFlowCode = "G1", FOrgId = 0, FIsTemplate = true, FStatus = "published" },
            new CfFlowDefinition { FFlowName = "组织伪模板", FFlowCode = "O1", FOrgId = 192, FIsTemplate = true, FStatus = "published" },
            new CfFlowDefinition { FFlowName = "全局非模板", FFlowCode = "G2", FOrgId = 0, FIsTemplate = false, FStatus = "published" });
        await db.SaveChangesAsync();

        var svc = new STOTOP.Module.CardFlow.Services.FlowDefinitionService(
            db, Microsoft.Extensions.Logging.Abstractions.NullLogger<STOTOP.Module.CardFlow.Services.FlowDefinitionService>.Instance);
        var templates = await svc.GetTemplatesAsync();

        Assert.Single(templates);
        Assert.Equal("G1", templates[0].FlowCode);
    }
}
