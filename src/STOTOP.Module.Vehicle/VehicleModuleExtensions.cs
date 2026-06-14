using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STOTOP.Module.Vehicle.Configurations;
using STOTOP.Module.Vehicle.Services;
using STOTOP.Module.Vehicle.Services.Interfaces;

namespace STOTOP.Module.Vehicle;

public static class VehicleModuleExtensions
{
    /// <summary>
    /// 添加车辆管理模块服务
    /// </summary>
    public static IServiceCollection AddVehicleModule(this IServiceCollection services)
    {
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IRentalStandardService, RentalStandardService>();
        services.AddScoped<IRentalChargeService, RentalChargeService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<IGpsService, GpsService>();

        return services;
    }

    /// <summary>
    /// 配置车辆管理模块的EF Core实体
    /// </summary>
    public static ModelBuilder ApplyVehicleConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new VehVehicleConfiguration());
        modelBuilder.ApplyConfiguration(new VehAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new VehRentalStandardConfiguration());
        modelBuilder.ApplyConfiguration(new VehRentalChargeConfiguration());
        modelBuilder.ApplyConfiguration(new VehMaintenanceConfiguration());
        modelBuilder.ApplyConfiguration(new VehInsuranceConfiguration());

        return modelBuilder;
    }
}
