using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STOTOP.Core.Interfaces;
using STOTOP.Infrastructure.Data;
using STOTOP.Infrastructure.Events;
using STOTOP.Module.Finance.Configurations;
using STOTOP.Module.Finance.EventHandlers;
using STOTOP.Module.Finance.Events;
using STOTOP.Module.Finance.Services;
using STOTOP.Module.Finance.Services.FormulaEngine;
using STOTOP.Module.Finance.Services.Interfaces;

namespace STOTOP.Module.Finance;

public static class FinanceModuleExtensions
{
    /// <summary>
    /// 添加财务模块服务
    /// </summary>
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        // 注册服务
        services.AddScoped<IAccountPeriodService, AccountPeriodService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAuxiliaryService, AuxiliaryService>();
        services.AddScoped<STOTOP.Module.Finance.Services.Interfaces.IVoucherService, VoucherService>();
        // CardFlow IVoucherService 桥接实现（供 CardFlow 模块跳过直接依赖调用凭证创建/红冲）
        services.AddScoped<STOTOP.Core.Interfaces.IVoucherService, CardFlowVoucherBridge>();
        services.AddScoped<VoucherRevokeHandler>();
        services.AddScoped<IDataScopeRevokeHandler>(sp => sp.GetRequiredService<VoucherRevokeHandler>());
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAmoebaService, AmoebaService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<AmoebaPLService>();
        services.AddScoped<ICommonCostAllocationEngine, CommonCostAllocationEngine>();
        services.AddScoped<AccountSetService>();
        services.AddScoped<JournalService>();
        services.AddScoped<OperationLogService>();
        services.AddScoped<ChangeTrackingService>();
        services.AddScoped<AttachmentService>();
        services.AddScoped<VoucherTemplateService>();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddSingleton<IFormulaEngine, FormulaEngineImpl>();
        services.AddScoped<IFormulaService, FormulaService>();
        services.AddScoped<IBankReconciliationService, BankReconciliationService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ITrialBalanceService, TrialBalanceService>();
        services.AddScoped<IBankTransactionService, BankTransactionService>();
        services.AddScoped<IVoucherAutoService, VoucherAutoService>();
        services.AddScoped<IAccountTemplateService, AccountTemplateService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IBudgetExpenseMappingService, BudgetExpenseMappingService>();
        services.AddScoped<ITreasuryPlanService, TreasuryPlanService>();
        services.AddScoped<IBudgetOccupationService, BudgetOccupationService>();
        services.AddScoped<VoucherExcelService>();
        services.AddScoped<AuxiliaryAliasService>();
        services.AddScoped<IAccountSetAuthorizationService, AccountSetAuthorizationService>();
        services.AddScoped<MigrationMappingService>();

        // 事件处理器
        services.AddScoped<IEventHandler<VoucherPendingAuditEvent>, VoucherPendingAuditEventHandler>();
        services.AddScoped<IEventHandler<AccountPeriodClosedEvent>, AccountPeriodClosedEventHandler>();
        services.AddScoped<IEventHandler<AuxiliarySourceChangedEvent>, AuxiliarySourceChangedHandler>();

        return services;
    }

    /// <summary>
    /// 配置财务模块的EF Core实体
    /// </summary>
    public static ModelBuilder ApplyFinanceConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FinAccountPeriodConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountConfiguration());
        modelBuilder.ApplyConfiguration(new FinAuxiliaryTypeConfiguration());
        modelBuilder.ApplyConfiguration(new FinAuxiliaryItemConfiguration());
        modelBuilder.ApplyConfiguration(new FinVoucherConfiguration());
        modelBuilder.ApplyConfiguration(new FinVoucherEntryConfiguration());
        modelBuilder.ApplyConfiguration(new FinAssetCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new FinAssetCardConfiguration());
        modelBuilder.ApplyConfiguration(new FinAmoebaPLTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new FinAmoebaPLItemConfiguration());
        modelBuilder.ApplyConfiguration(new FinAmoebaMappingRuleConfiguration());
        modelBuilder.ApplyConfiguration(new FinAmoebaAllocationConfiguration());
        modelBuilder.ApplyConfiguration(new FinAmoebaManualClassifyConfiguration());
        modelBuilder.ApplyConfiguration(new FinAmoebaManualDataConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new FinAuxiliaryBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountSetConfiguration());
        modelBuilder.ApplyConfiguration(new FinOperationLogConfiguration());
        modelBuilder.ApplyConfiguration(new FinAttachmentConfiguration());
        modelBuilder.ApplyConfiguration(new FinVoucherTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new FinVoucherTemplateEntryConfiguration());
        modelBuilder.ApplyConfiguration(new FinExchangeRateConfiguration());
        modelBuilder.ApplyConfiguration(new FinReportFormulaConfiguration());
        modelBuilder.ApplyConfiguration(new FinBankStatementConfiguration());
        modelBuilder.ApplyConfiguration(new FinBankReconciliationConfiguration());
        modelBuilder.ApplyConfiguration(new FinInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new FinChangeHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new FinTrialBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new FinPaymentChannelConfiguration());
        modelBuilder.ApplyConfiguration(new FinBankTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new FinVoucherRuleConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountTemplateItemConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountSetRoleConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountSetRolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountSetAuthorizationConfiguration());
        modelBuilder.ApplyConfiguration(new FinMigrationSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new FinAccountMappingDetailConfiguration());
        modelBuilder.ApplyConfiguration(new FinAuxMappingDetailConfiguration());
        modelBuilder.ApplyConfiguration(new FinAssetMappingDetailConfiguration());
        modelBuilder.ApplyConfiguration(new FinVoucherAssetLinkConfiguration());
        modelBuilder.ApplyConfiguration(new FinAuxiliaryAliasConfiguration());
        modelBuilder.ApplyConfiguration(new FinBudgetVersionConfiguration());
        modelBuilder.ApplyConfiguration(new FinBudgetLineConfiguration());
        modelBuilder.ApplyConfiguration(new FinBudgetExpenseMappingConfiguration());
        modelBuilder.ApplyConfiguration(new FinTreasuryAccountBindingConfiguration());
        modelBuilder.ApplyConfiguration(new FinTreasuryPlanLineConfiguration());
        modelBuilder.ApplyConfiguration(new FinBudgetOccupationConfiguration());

        return modelBuilder;
    }
}
