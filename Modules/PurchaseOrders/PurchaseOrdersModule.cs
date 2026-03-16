using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.PurchaseOrders.Services;
using StoreAssistantPro.Modules.PurchaseOrders.ViewModels;

namespace StoreAssistantPro.Modules.PurchaseOrders;

public static class PurchaseOrdersModule
{
    public const string PurchaseOrdersPage = "PurchaseOrders";

    public static IServiceCollection AddPurchaseOrdersModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<PurchaseOrderViewModel>(PurchaseOrdersPage)
            .RequireFeature(PurchaseOrdersPage, FeatureFlags.PurchaseOrders);
        services.AddTransient<IPurchaseOrderService, PurchaseOrderService>();
        services.AddTransient<PurchaseOrderViewModel>();
        return services;
    }
}
