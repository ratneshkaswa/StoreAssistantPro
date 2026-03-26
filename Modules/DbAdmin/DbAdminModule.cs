using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.DbAdmin.Services;

namespace StoreAssistantPro.Modules.DbAdmin;

public static class DbAdminModule
{
    public static IServiceCollection AddDbAdminModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IDatabaseMonitorService, DatabaseMonitorService>();
        services.AddTransient<IMigrationManagerService, MigrationManagerService>();
        services.AddTransient<IDataManagementService, DataManagementService>();
        services.AddTransient<IDataTransferService, DataTransferService>();

        // Holds maintenance schedule state → Singleton.
        services.AddSingleton<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        return services;
    }
}
