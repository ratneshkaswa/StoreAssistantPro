using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.MultiStore.Services;

namespace StoreAssistantPro.Modules.MultiStore;

public static class MultiStoreModule
{
    public static IServiceCollection AddMultiStoreModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IStoreManagementService, StoreManagementService>();
        services.AddTransient<IStockTransferService, StockTransferService>();
        // Sync service holds connection/sync state → Singleton.
        services.AddSingleton<ISyncService, SyncService>();
        return services;
    }
}
