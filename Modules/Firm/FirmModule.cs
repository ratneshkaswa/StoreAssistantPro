using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Firm.Services;
using StoreAssistantPro.Modules.Firm.ViewModels;
using StoreAssistantPro.Modules.Firm.Views;

namespace StoreAssistantPro.Modules.Firm;

public static class FirmModule
{
    public const string FirmManagementDialog = "FirmManagement";

    public static IServiceCollection AddFirmModule(this IServiceCollection services)
    {
        // Services
        services.AddTransient<IFirmService, FirmService>();

        // ViewModels
        services.AddTransient<FirmViewModel>();

        // Views
        services.AddTransient<FirmWindow>();

        // Dialog registration
        services.AddDialogRegistration<FirmWindow>(FirmManagementDialog);

        return services;
    }
}
