using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Api.Services;

namespace StoreAssistantPro.Modules.Api;

public static class ApiModule
{
    public static IServiceCollection AddApiModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IProductApiService, ProductApiService>();
        services.AddTransient<ISaleApiService, SaleApiService>();
        services.AddTransient<ICustomerApiService, CustomerApiService>();
        services.AddTransient<IInventoryApiService, InventoryApiService>();
        services.AddTransient<IAccountingExportService, AccountingExportService>();

        // State-holding services → Singleton.
        services.AddSingleton<IApiAuthService, ApiAuthService>();
        services.AddSingleton<IApiRateLimitService, ApiRateLimitService>();
        services.AddSingleton<ICommunicationService, CommunicationService>();
        return services;
    }
}
