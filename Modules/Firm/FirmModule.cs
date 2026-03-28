using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Firm.Services;
using StoreAssistantPro.Modules.Firm.ViewModels;

namespace StoreAssistantPro.Modules.Firm;

public static class FirmModule
{
    public const string FirmManagementPage = "FirmManagement";

    public static IServiceCollection AddFirmModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<FirmViewModel>(FirmManagementPage)
            .RequireFeature(FirmManagementPage, FeatureFlags.FirmManagement);
        pageRegistry.CachePage(FirmManagementPage);
        services.AddTransient<IFirmService, FirmService>();
        services.AddTransient<FirmViewModel>();
        return services;
    }
}
