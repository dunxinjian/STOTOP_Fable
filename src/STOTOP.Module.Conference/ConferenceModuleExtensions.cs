using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STOTOP.Module.Conference.Configurations;
using STOTOP.Module.Conference.Services;
using STOTOP.Module.Conference.Services.Interfaces;

namespace STOTOP.Module.Conference;

public static class ConferenceModuleExtensions
{
    /// <summary>
    /// 添加会务管理模块服务
    /// </summary>
    public static IServiceCollection AddConferenceModule(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IAttendeeService, AttendeeService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<ITransportService, TransportService>();
        services.AddScoped<IVehicleScheduleService, VehicleScheduleService>();
        services.AddScoped<IAccommodationService, AccommodationService>();
        services.AddScoped<IMealService, MealService>();
        services.AddScoped<ITableArrangementService, TableArrangementService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IGiftService, GiftService>();
        services.AddScoped<ICeremonyService, CeremonyService>();
        return services;
    }

    /// <summary>
    /// 配置会务管理模块的EF Core实体
    /// </summary>
    public static ModelBuilder ApplyConferenceConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConfEventConfiguration());
        modelBuilder.ApplyConfiguration(new ConfAttendeeConfiguration());
        modelBuilder.ApplyConfiguration(new ConfScheduleConfiguration());
        modelBuilder.ApplyConfiguration(new ConfScheduleAttendeeConfiguration());
        modelBuilder.ApplyConfiguration(new ConfScheduleItemConfiguration());
        modelBuilder.ApplyConfiguration(new ConfVehicleConfiguration());
        modelBuilder.ApplyConfiguration(new ConfPickupTaskConfiguration());
        modelBuilder.ApplyConfiguration(new ConfPickupPassengerConfiguration());
        modelBuilder.ApplyConfiguration(new ConfHotelConfiguration());
        modelBuilder.ApplyConfiguration(new ConfRoomConfiguration());
        modelBuilder.ApplyConfiguration(new ConfRoomGuestConfiguration());
        modelBuilder.ApplyConfiguration(new ConfMealPlanConfiguration());
        modelBuilder.ApplyConfiguration(new ConfMealAttendeeConfiguration());
        modelBuilder.ApplyConfiguration(new ConfTableConfiguration());
        modelBuilder.ApplyConfiguration(new ConfTableSeatConfiguration());
        modelBuilder.ApplyConfiguration(new ConfIncomeConfiguration());
        modelBuilder.ApplyConfiguration(new ConfMaterialConfiguration());
        modelBuilder.ApplyConfiguration(new ConfVehicleScheduleConfiguration());

        return modelBuilder;
    }
}
