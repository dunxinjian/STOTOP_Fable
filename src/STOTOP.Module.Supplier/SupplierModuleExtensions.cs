using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STOTOP.Module.Supplier.Configurations;
using STOTOP.Module.Supplier.Services;
using STOTOP.Module.Supplier.Services.Interfaces;

namespace STOTOP.Module.Supplier;

public static class SupplierModuleExtensions
{
    /// <summary>
    /// 添加供应商模块服务
    /// </summary>
    public static IServiceCollection AddSupplierModule(this IServiceCollection services)
    {
        services.AddScoped<ISupplierService, SupplierService>();

        return services;
    }

    /// <summary>
    /// 配置供应商模块的EF Core实体
    /// </summary>
    public static ModelBuilder ApplySupplierConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SupSupplierConfiguration());
        modelBuilder.ApplyConfiguration(new SupBankAccountConfiguration());

        return modelBuilder;
    }
}
