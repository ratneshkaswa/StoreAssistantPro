using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Inventory.Services;

namespace StoreAssistantPro.Modules.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddSingleton<IStockAlertService, StockAlertService>();
        services.AddSingleton<StockAlertListener>();

        return services;
    }
}
