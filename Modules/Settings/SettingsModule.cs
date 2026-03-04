using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Settings.Services;
using StoreAssistantPro.Modules.Settings.ViewModels;
using StoreAssistantPro.Modules.Settings.Views;

namespace StoreAssistantPro.Modules.Settings;

public static class SettingsModule
{
    public const string SystemSettingsDialog = "SystemSettings";

    public static IServiceCollection AddSettingsModule(this IServiceCollection services)
    {
        services.AddTransient<ISystemSettingsService, SystemSettingsService>();
        services.AddTransient<SystemSettingsViewModel>();
        services.AddTransient<SystemSettingsWindow>();
        services.AddDialogRegistration<SystemSettingsWindow>(SystemSettingsDialog);
        return services;
    }
}
