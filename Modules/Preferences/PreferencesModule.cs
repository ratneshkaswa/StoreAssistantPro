using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Preferences.Services;

namespace StoreAssistantPro.Modules.Preferences;

public static class PreferencesModule
{
    public static IServiceCollection AddPreferencesModule(this IServiceCollection services)
    {
        // DB-accessing service with no mutable state → Transient.
        services.AddTransient<IUserPreferenceDbService, UserPreferenceDbService>();
        return services;
    }
}
