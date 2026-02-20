using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.SystemSettings.Commands;
using StoreAssistantPro.Modules.SystemSettings.Services;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;
using StoreAssistantPro.Modules.SystemSettings.Views;
using StoreAssistantPro.Modules.SystemSettings.Workflows;

namespace StoreAssistantPro.Modules.SystemSettings;

public static class SystemSettingsModule
{
    public const string SystemSettingsDialog = "SystemSettings";

    public static IServiceCollection AddSystemSettingsModule(this IServiceCollection services)
    {
        // Services
        services.AddTransient<ISystemSettingsService, SystemSettingsService>();

        // Workflows
        services.AddSingleton<IWorkflow, SettingsWorkflow>();

        // Command handlers
        services.AddTransient<ICommandHandler<ChangeMasterPinCommand>, ChangeMasterPinHandler>();

        // ViewModels
        services.AddTransient<SystemSettingsViewModel>();
        services.AddTransient<GeneralSettingsViewModel>();
        services.AddTransient<SecuritySettingsViewModel>();
        services.AddTransient<BackupSettingsViewModel>();
        services.AddTransient<AppInfoViewModel>();

        // Views
        services.AddTransient<SystemSettingsWindow>();

        // Dialog registration
        services.AddDialogRegistration<SystemSettingsWindow>(SystemSettingsDialog);

        return services;
    }
}
