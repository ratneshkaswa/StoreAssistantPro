using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Mobile.Services;

namespace StoreAssistantPro.Modules.Mobile;

public static class MobileModule
{
    public static IServiceCollection AddMobileModule(this IServiceCollection services)
    {
        // DB-accessing service with no mutable state → Transient.
        services.AddTransient<IMobileCompanionService, MobileCompanionService>();
        return services;
    }
}
