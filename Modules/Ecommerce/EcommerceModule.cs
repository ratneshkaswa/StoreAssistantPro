using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Ecommerce.Services;

namespace StoreAssistantPro.Modules.Ecommerce;

public static class EcommerceModule
{
    public static IServiceCollection AddEcommerceModule(this IServiceCollection services)
    {
        // Platform connection holds live state → Singleton.
        services.AddSingleton<IPlatformConnectionService, PlatformConnectionService>();
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IProductSyncService, ProductSyncService>();
        services.AddTransient<IOnlineOrderService, OnlineOrderService>();
        services.AddTransient<IMarketplaceService, MarketplaceService>();
        return services;
    }
}
