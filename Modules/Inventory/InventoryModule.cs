using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Inventory.Services;
using StoreAssistantPro.Modules.Inventory.ViewModels;

namespace StoreAssistantPro.Modules.Inventory;

public static class InventoryModule
{
    public const string InventoryPage = "Inventory";

    public static IServiceCollection AddInventoryModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<InventoryViewModel>(InventoryPage)
            .RequireFeature(InventoryPage, FeatureFlags.Inventory);
        services.AddTransient<IInventoryService, InventoryService>();
        services.AddTransient<InventoryViewModel>();
        return services;
    }
}
