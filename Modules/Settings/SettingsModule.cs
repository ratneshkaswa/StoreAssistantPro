using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Settings.Services;
using StoreAssistantPro.Modules.Settings.ViewModels;

namespace StoreAssistantPro.Modules.Settings;

public static class SettingsModule
{
    public const string SystemSettingsPage = "SystemSettings";

    public static IServiceCollection AddSettingsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<SystemSettingsViewModel>(SystemSettingsPage)
            .RequireFeature(SystemSettingsPage, FeatureFlags.SystemSettings);
        pageRegistry.CachePage(SystemSettingsPage);
        services.AddTransient<ISystemSettingsService, SystemSettingsService>();
        services.AddTransient<SystemSettingsViewModel>();
        return services;
    }
}
