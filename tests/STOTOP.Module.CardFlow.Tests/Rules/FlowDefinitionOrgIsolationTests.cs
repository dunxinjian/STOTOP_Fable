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

    [Fact]
    public async global::System.Threading.Tasks.Task CloneFlowDefinitionAsync_IgnoresRequestOrgId()
    {
        using var db = TestDbContextFactory.Create(nameof(CloneFlowDefinitionAsync_IgnoresRequestOrgId), null); // accessor=null → FillOrgId no-op → FOrgId stays 0
        db.Set<CfFlowDefinition>().Add(new CfFlowDefinition { FFlowName = "源", FFlowCode = "SRC1", FOrgId = 0, FStatus = "published" });
        await db.SaveChangesAsync();
        var srcId = db.Set<CfFlowDefinition>().IgnoreQueryFilters().Single(x => x.FFlowCode == "SRC1").FID;

        var svc = new STOTOP.Module.CardFlow.Services.FlowDefinitionService(
            db, Microsoft.Extensions.Logging.Abstractions.NullLogger<STOTOP.Module.CardFlow.Services.FlowDefinitionService>.Instance);
        await svc.CloneFlowDefinitionAsync(srcId,
            new STOTOP.Module.CardFlow.Dtos.CloneFlowDefinitionRequest { FlowName = "克隆", FlowCode = "CLONE1", OrgId = 999 }, operatorId: 1);

        var cloned = db.Set<CfFlowDefinition>().IgnoreQueryFilters().Single(x => x.FFlowCode == "CLONE1");
        Assert.NotEqual(999, cloned.FOrgId);   // request.OrgId=999 ignored
        Assert.Equal(0, cloned.FOrgId);        // accessor=null → stays 0 (prod: FillOrgId sets current org; see Task 5)
    }

    [Fact]
    public async global::System.Threading.Tasks.Task CloneFlowDefinitionAsync_OtherOrgPrivateSource_NotFound()
    {
        using var db = TestDbContextFactory.Create(nameof(CloneFlowDefinitionAsync_OtherOrgPrivateSource_NotFound), 192);
        using (db.SuppressOrgIdFill())
        {
            db.Set<CfFlowDefinition>().Add(new CfFlowDefinition { FFlowName = "他组织私有", FFlowCode = "OTHER1", FOrgId = 888, FStatus = "published" });
            db.SaveChanges();
        }
        var otherId = db.Set<CfFlowDefinition>().IgnoreQueryFilters().Single(x => x.FFlowCode == "OTHER1").FID;

        var svc = new STOTOP.Module.CardFlow.Services.FlowDefinitionService(
            db, Microsoft.Extensions.Logging.Abstractions.NullLogger<STOTOP.Module.CardFlow.Services.FlowDefinitionService>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CloneFlowDefinitionAsync(otherId,
            new STOTOP.Module.CardFlow.Dtos.CloneFlowDefinitionRequest { FlowName = "x", FlowCode = "x" }, 1));
    }

    [Fact]
    public async global::System.Threading.Tasks.Task CloneFlowDefinitionAsync_GlobalTemplateSource_Works()
    {
        using var db = TestDbContextFactory.Create(nameof(CloneFlowDefinitionAsync_GlobalTemplateSource_Works), 192);
        using (db.SuppressOrgIdFill())
        {
            db.Set<CfFlowDefinition>().Add(new CfFlowDefinition { FFlowName = "全局模板", FFlowCode = "GT1", FOrgId = 0, FIsTemplate = true, FStatus = "published" });
            db.SaveChanges();
        }
        var tplId = db.Set<CfFlowDefinition>().IgnoreQueryFilters().Single(x => x.FFlowCode == "GT1").FID;

        var svc = new STOTOP.Module.CardFlow.Services.FlowDefinitionService(
            db, Microsoft.Extensions.Logging.Abstractions.NullLogger<STOTOP.Module.CardFlow.Services.FlowDefinitionService>.Instance);
        var dto = await svc.CloneFlowDefinitionAsync(tplId,
            new STOTOP.Module.CardFlow.Dtos.CloneFlowDefinitionRequest { FlowName = "从模板", FlowCode = "FROMTPL1" }, 1);

        Assert.NotNull(dto);
        var cloned = db.Set<CfFlowDefinition>().IgnoreQueryFilters().Single(x => x.FFlowCode == "FROMTPL1");
        Assert.Equal(192, cloned.FOrgId); // 克隆落当前组织
    }
}
