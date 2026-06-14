using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STOTOP.Module.OA.Services;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA;

public static class OAModuleExtensions
{
    /// <summary>
    /// 添加OA模块服务
    /// </summary>
    public static IServiceCollection AddOAModule(this IServiceCollection services)
    {
        // HttpClientFactory（钉钉 API 调用）
        services.AddHttpClient();

        // 钉钉集成服务
        services.AddScoped<IDingTalkAuthService, DingTalkAuthService>();
        services.AddScoped<IDingTalkCalendarService, DingTalkCalendarService>();

        // 通用委托配置
        services.AddScoped<IDelegationService, DelegationService>();

        // 单据服务
        services.AddScoped<IExpenseRequestService, ExpenseRequestService>();
        services.AddScoped<IExpenseReimburseService, ExpenseReimburseService>();
        services.AddScoped<IExternalPaymentService, ExternalPaymentService>();
        services.AddScoped<IPettyCashService, PettyCashService>();
        services.AddScoped<ISalaryAdvanceService, SalaryAdvanceService>();
        services.AddScoped<ILoanApplyService, LoanApplyService>();

        // 附件服务
        services.AddScoped<IAttachmentService, AttachmentService>();

        // 日历服务
        services.AddScoped<ICalendarEventService, CalendarEventService>();

        // 配置服务
        services.AddScoped<IExpenseTypeService, ExpenseTypeService>();
        services.AddScoped<IExpenseAccountMappingService, ExpenseAccountMappingService>();

        return services;
    }

    /// <summary>
    /// 配置OA模块的EF Core实体
    /// </summary>
    public static ModelBuilder ApplyOAConfigurations(this ModelBuilder modelBuilder)
    {
        // OA 实体配置通过 ApplyConfigurationsFromAssembly 自动发现注册
        // 所有 IEntityTypeConfiguration<T> 实现已在 Configurations 目录中定义
        return modelBuilder;
    }
}
