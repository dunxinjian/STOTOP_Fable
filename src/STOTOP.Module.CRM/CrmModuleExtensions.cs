using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STOTOP.Module.CRM.Configurations;
using STOTOP.Module.CRM.Services;
using STOTOP.Module.CRM.Services.Interfaces;

namespace STOTOP.Module.CRM;

public static class CrmModuleExtensions
{
    /// <summary>
    /// 添加CRM模块服务
    /// </summary>
    public static IServiceCollection AddCrmModule(this IServiceCollection services)
    {
        services.AddScoped<ICrmOrgService, CrmOrgService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IVisitRecordService, VisitRecordService>();
        services.AddScoped<IServiceOrderService, ServiceOrderService>();
        services.AddScoped<IServiceFeedbackService, ServiceFeedbackService>();
        services.AddScoped<IReferralCommissionService, ReferralCommissionService>();
        services.AddScoped<IPrepaymentWaybillService, PrepaymentWaybillService>();
        services.AddScoped<IProfitCalcService, ProfitCalcService>();
        services.AddScoped<IBonusService, BonusService>();
        return services;
    }

    /// <summary>
    /// 配置CRM模块的EF Core实体
    /// </summary>
    public static ModelBuilder ApplyCrmConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CrmExternalContactConfiguration());
        modelBuilder.ApplyConfiguration(new CrmCustomerConfiguration());
        modelBuilder.ApplyConfiguration(new CrmCustomerContactConfiguration());
        modelBuilder.ApplyConfiguration(new CrmRoleMappingConfiguration());
        modelBuilder.ApplyConfiguration(new CrmCustomerTransferConfiguration());
        modelBuilder.ApplyConfiguration(new CrmVisitRecordConfiguration());
        modelBuilder.ApplyConfiguration(new CrmServiceOrderConfiguration());
        modelBuilder.ApplyConfiguration(new CrmServiceOrderLogConfiguration());
        modelBuilder.ApplyConfiguration(new CrmCustomerProfitConfiguration());
        modelBuilder.ApplyConfiguration(new CrmBonusPlanConfiguration());
        modelBuilder.ApplyConfiguration(new CrmBonusDetailConfiguration());
        modelBuilder.ApplyConfiguration(new CrmServiceFeedbackConfiguration());
        modelBuilder.ApplyConfiguration(new CrmReferralConfiguration());
        modelBuilder.ApplyConfiguration(new CrmCommissionConfiguration());
        modelBuilder.ApplyConfiguration(new CrmWaybillPoolConfiguration());
        modelBuilder.ApplyConfiguration(new CrmCustomerAccountConfiguration());
        modelBuilder.ApplyConfiguration(new CrmPrepaymentConfiguration());
        modelBuilder.ApplyConfiguration(new CrmWaybillAllocationConfiguration());

        return modelBuilder;
    }
}
