using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Inventory.Services;
using StoreAssistantPro.Modules.Inventory.ViewModels;
using StoreAssistantPro.Modules.Inventory.Views;

namespace StoreAssistantPro.Modules.Inventory;

public static class InventoryModule
{
    public const string InventoryDialog = "Inventory";

    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddTransient<IInventoryService, InventoryService>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<InventoryWindow>();
        services.AddDialogRegistration<InventoryWindow>(InventoryDialog);
        return services;
    }
}
