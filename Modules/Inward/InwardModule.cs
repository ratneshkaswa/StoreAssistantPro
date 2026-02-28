using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Inward.Services;
using StoreAssistantPro.Modules.Inward.ViewModels;

namespace StoreAssistantPro.Modules.Inward;

public static class InwardModule
{
    public const string InwardPage = "Inward";

    public static IServiceCollection AddInwardModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<InwardViewModel>(InwardPage);

        services.AddTransient<IInwardService, InwardService>();
        services.AddTransient<InwardViewModel>();

        return services;
    }
}
