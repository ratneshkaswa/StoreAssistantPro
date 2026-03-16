using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Ironing.Services;
using StoreAssistantPro.Modules.Ironing.ViewModels;

namespace StoreAssistantPro.Modules.Ironing;

public static class IroningModule
{
    public const string IroningManagementPage = "IroningManagement";

    public static IServiceCollection AddIroningModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<IroningManagementViewModel>(IroningManagementPage)
            .RequireFeature(IroningManagementPage, FeatureFlags.Ironing);
        services.AddTransient<IIroningService, IroningService>();
        services.AddTransient<IroningManagementViewModel>();
        return services;
    }
}
