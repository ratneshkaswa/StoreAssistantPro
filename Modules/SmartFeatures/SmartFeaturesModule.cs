using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.SmartFeatures.Services;

namespace StoreAssistantPro.Modules.SmartFeatures;

public static class SmartFeaturesModule
{
    public static IServiceCollection AddSmartFeaturesModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<ISalesForecastingService, SalesForecastingService>();
        services.AddTransient<ISmartPricingService, SmartPricingService>();
        services.AddTransient<ICustomerInsightsService, CustomerInsightsService>();
        services.AddTransient<ISmartSearchService, SmartSearchService>();
        services.AddTransient<IAnomalyDetectionService, AnomalyDetectionService>();
        return services;
    }
}
