using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Orders.Services;
using StoreAssistantPro.Modules.Orders.ViewModels;

namespace StoreAssistantPro.Modules.Orders;

public static class OrdersModule
{
    public const string OrderManagementPage = "OrderManagement";

    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<OrderManagementViewModel>(OrderManagementPage)
            .RequireFeature(OrderManagementPage, FeatureFlags.Orders);
        services.AddTransient<IOrderService, OrderService>();
        services.AddTransient<OrderManagementViewModel>();
        return services;
    }
}
