using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.PurchaseOrders.Services;
using StoreAssistantPro.Modules.PurchaseOrders.ViewModels;
using StoreAssistantPro.Modules.PurchaseOrders.Views;

namespace StoreAssistantPro.Modules.PurchaseOrders;

public static class PurchaseOrdersModule
{
    public const string PurchaseOrderDialog = "PurchaseOrders";

    public static IServiceCollection AddPurchaseOrdersModule(this IServiceCollection services)
    {
        services.AddTransient<IPurchaseOrderService, PurchaseOrderService>();
        services.AddTransient<PurchaseOrderViewModel>();
        services.AddTransient<PurchaseOrderWindow>();
        services.AddDialogRegistration<PurchaseOrderWindow>(PurchaseOrderDialog);
        return services;
    }
}
