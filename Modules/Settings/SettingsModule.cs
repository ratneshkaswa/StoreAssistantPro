using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Settings.Services;

namespace StoreAssistantPro.Modules.Settings;

public static class SettingsModule
{
    public static IServiceCollection AddSettingsModule(this IServiceCollection services)
    {
        services.AddTransient<ISystemSettingsService, SystemSettingsService>();
        return services;
    }
}
