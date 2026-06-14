using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STOTOP.Module.Insurance.Configurations;
using STOTOP.Module.Insurance.Services;
using STOTOP.Module.Insurance.Services.Interfaces;

namespace STOTOP.Module.Insurance;

public static class InsuranceModuleExtensions
{
    /// <summary>
    /// 添加保险管理模块服务
    /// </summary>
    public static IServiceCollection AddInsuranceModule(this IServiceCollection services)
    {
        // 保险公司、保单、出险
        services.AddScoped<IInsCompanyService, InsCompanyService>();
        services.AddScoped<IInsPolicyService, InsPolicyService>();
        services.AddScoped<IInsClaimService, InsClaimService>();
        // 理赔审批、共保基金、审批配置、报表
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<ICoInsuranceFundService, CoInsuranceFundService>();
        services.AddScoped<IApprovalConfigService, ApprovalConfigService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }

    /// <summary>
    /// 配置保险管理模块的EF Core实体
    /// </summary>
    public static ModelBuilder ApplyInsuranceConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InsCompanyConfiguration());
        modelBuilder.ApplyConfiguration(new InsPolicyConfiguration());
        modelBuilder.ApplyConfiguration(new InsClaimConfiguration());
        modelBuilder.ApplyConfiguration(new InsSettlementConfiguration());
        modelBuilder.ApplyConfiguration(new InsCoInsuranceFundConfiguration());
        modelBuilder.ApplyConfiguration(new InsFundContributionConfiguration());
        modelBuilder.ApplyConfiguration(new InsApprovalConfigConfiguration());
        modelBuilder.ApplyConfiguration(new InsApprovalRecordConfiguration());

        return modelBuilder;
    }
}
