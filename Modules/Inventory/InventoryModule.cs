using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Inventory.Services;
using StoreAssistantPro.Modules.Inventory.ViewModels;

namespace StoreAssistantPro.Modules.Inventory;

public static class InventoryModule
{
    public const string StockAlertsPage = "StockAlerts";

    public static IServiceCollection AddInventoryModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<StockAlertsViewModel>(StockAlertsPage);

        services.AddSingleton<IStockAlertService, StockAlertService>();
        services.AddSingleton<StockAlertListener>();
        services.AddTransient<StockAlertsViewModel>();

        return services;
    }
}
