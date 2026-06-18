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
}
