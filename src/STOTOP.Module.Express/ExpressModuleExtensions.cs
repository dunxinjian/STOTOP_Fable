using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STOTOP.Infrastructure.Events;
using STOTOP.Module.CardFlow.AutoPlugin;
using STOTOP.Module.Express.Configurations;
using STOTOP.Module.Express.EventHandlers;
using STOTOP.Module.Express.Services;
using STOTOP.Module.Express.Services.Agents;
using STOTOP.Module.Express.Services.Billing;

namespace STOTOP.Module.Express;

public static class ExpressModuleExtensions
{
    /// <summary>
    /// 添加快递模块服务
    /// </summary>
    public static IServiceCollection AddExpressModule(this IServiceCollection services)
    {
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IProvinceService, ProvinceService>();
        services.AddScoped<INetworkPointService, NetworkPointService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IFranchiseAreaService, FranchiseAreaService>();
        services.AddScoped<ILastMileStationService, LastMileStationService>();
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<IPricePlanImportService, PricePlanImportService>();
        services.AddScoped<IPriceSurchargeService, PriceSurchargeService>();
        services.AddScoped<ICostItemService, CostItemService>();
        services.AddScoped<ICostPlanService, CostPlanService>();
        services.AddScoped<IWaybillService, WaybillService>();
        services.AddScoped<IWaybillImportService, WaybillImportService>();
        services.AddScoped<ShopAutoDiscoveryJob>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<PricingEngine>();
        services.AddScoped<CostEngine>();
        services.AddScoped<BillingBulkWriter>();
        // 导入计算验证工作台的价格解释（接口定义在 CardFlow，避免 CardFlow→Express 反向依赖）
        services.AddScoped<STOTOP.Module.CardFlow.Services.Validation.IPricingExplainProvider, PricingExplainProvider>();
        services.AddSingleton<ProvinceCache>();
        services.AddScoped<PricingPlugin>();
        services.AddScoped<IQualityIssueTypeProvider>(sp => sp.GetRequiredService<PricingPlugin>());
        services.AddScoped<CostPlugin>();
        services.AddScoped<IQualityIssueTypeProvider>(sp => sp.GetRequiredService<CostPlugin>());

        // 账单管理
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoiceReviewService, InvoiceReviewService>();
        services.AddScoped<InvoiceGeneratorJob>();
        // 预付款管理
        services.AddScoped<IPrepaymentService, PrepaymentService>();
        // 运单号管理
        services.AddScoped<IWaybillNumberService, WaybillNumberService>();
        // 政策返利
        services.AddScoped<IPolicyRebateService, PolicyRebateService>();
        services.AddScoped<PolicyRebateCalcEngine>();
        services.AddScoped<IPolicyRebateSettlementService, PolicyRebateSettlementService>();
        services.AddScoped<PolicyRebateSimulator>();

        // 归档
        services.AddScoped<IWaybillArchiveService, WaybillArchiveService>();
        // 统计报表
        services.AddScoped<IFlowAnalysisService, FlowAnalysisService>();
        services.AddScoped<IWeightSegmentReportService, WeightSegmentReportService>();
        services.AddScoped<IProfitAnalysisService, ProfitAnalysisService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // 数据质量中心
        services.AddScoped<IQualityCenterService, QualityCenterService>();

        // 事件处理器
        services.AddScoped<IEventHandler<WorkItemStatusChangedEvent>, WorkItemStatusChangedHandler>();

        // 业务员管理
        services.AddScoped<ISalesmanService, SalesmanService>();
        // 用户网点权限
        services.AddScoped<IUserNetworkPermissionService, UserNetworkPermissionService>();

        return services;
    }

    /// <summary>
    /// 配置快递模块的EF Core实体
    /// </summary>
    public static ModelBuilder ApplyExpressConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ExpBrandConfiguration());
        modelBuilder.ApplyConfiguration(new ExpShopConfiguration());
        modelBuilder.ApplyConfiguration(new ExpProvinceConfiguration());
        modelBuilder.ApplyConfiguration(new ExpCityConfiguration());
        modelBuilder.ApplyConfiguration(new ExpNetworkPointConfiguration());
        modelBuilder.ApplyConfiguration(new ExpAgentConfiguration());
        modelBuilder.ApplyConfiguration(new ExpFranchiseAreaConfiguration());
        modelBuilder.ApplyConfiguration(new ExpLastMileStationConfiguration());
        modelBuilder.ApplyConfiguration(new ExpClientFeeWaiverConfiguration());
        modelBuilder.ApplyConfiguration(new ExpQuotationConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPriceSurchargeConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPriceSurchargeItemConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPriceSurchargeItemDestConfiguration());
        modelBuilder.ApplyConfiguration(new ExpCostItemConfiguration());
        modelBuilder.ApplyConfiguration(new ExpCostPlanConfiguration());
        modelBuilder.ApplyConfiguration(new ExpWaybillConfiguration());
        modelBuilder.ApplyConfiguration(new ExpBillingResultConfiguration());
        modelBuilder.ApplyConfiguration(new ExpBillingCostBreakdownConfiguration());

        // 报价重构新实体
        modelBuilder.ApplyConfiguration(new ExpQuotationShopConfiguration());
        modelBuilder.ApplyConfiguration(new ExpQuotationCommissionConfiguration());
        modelBuilder.ApplyConfiguration(new ExpQuotationAliasConfiguration());

        // 账单相关
        modelBuilder.ApplyConfiguration(new ExpInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new ExpInvoiceReviewRuleConfiguration());
        modelBuilder.ApplyConfiguration(new ExpInvoiceReviewLogConfiguration());
        modelBuilder.ApplyConfiguration(new ExpClientWeightCapConfiguration());
        modelBuilder.ApplyConfiguration(new ExpClientProvinceQuotaConfiguration());
        // 预付款相关
        modelBuilder.ApplyConfiguration(new ExpPrepaymentConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPrepaymentBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPrepaymentTransactionConfiguration());
        // 运单号相关
        modelBuilder.ApplyConfiguration(new ExpWaybillNumberPoolConfiguration());
        modelBuilder.ApplyConfiguration(new ExpWaybillNumberTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new ExpClientWaybillBalanceConfiguration());
        // 政策返利相关
        modelBuilder.ApplyConfiguration(new ExpPolicyRebateConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPolicyRebateTierConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPolicyRebateRuleConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPolicyRebateRuleItemConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPolicyRebateSettlementConfiguration());
        modelBuilder.ApplyConfiguration(new ExpPolicyRebateSettlementDetailConfiguration());

        // 客户返利相关
        modelBuilder.ApplyConfiguration(new ExpClientRebateConfiguration());
        modelBuilder.ApplyConfiguration(new ExpClientRebateTierConfiguration());

        // 月度调整
        modelBuilder.ApplyConfiguration(new ExpMonthlyAdjustmentConfiguration());

        // 历史归档表
        modelBuilder.ApplyConfiguration(new ExpWaybillHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new ExpBillingResultHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new ExpBillingCostBreakdownHistoryConfiguration());

        // 发件量阶梯与报价加收关联
        modelBuilder.ApplyConfiguration(new ExpVolumeTierConfiguration());
        modelBuilder.ApplyConfiguration(new ExpQuotationSurchargeLinkConfiguration());

        // 成本方案新架构（重构后）
        modelBuilder.ApplyConfiguration(new ExpCostPlanItemConfiguration());
        modelBuilder.ApplyConfiguration(new ExpCostPlanItemOutletConfiguration());
        modelBuilder.ApplyConfiguration(new ExpCostPlanItemShopConfiguration());
        modelBuilder.ApplyConfiguration(new ExpCostPlanItemPeriodConfiguration());
        modelBuilder.ApplyConfiguration(new ExpCostPlanExclusionConfiguration());

        return modelBuilder;
    }
}
