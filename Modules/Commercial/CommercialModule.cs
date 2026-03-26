using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Commercial.Services;

namespace StoreAssistantPro.Modules.Commercial;

public static class CommercialModule
{
    public static IServiceCollection AddCommercialModule(this IServiceCollection services)
    {
        // License/subscription state is cached → Singleton.
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<IWhiteLabelService, WhiteLabelService>();
        // Usage analytics accumulates events → Singleton.
        services.AddSingleton<IUsageAnalyticsService, UsageAnalyticsService>();
        return services;
    }
}
