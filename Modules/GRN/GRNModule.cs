using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.GRN.Services;
using StoreAssistantPro.Modules.GRN.ViewModels;

namespace StoreAssistantPro.Modules.GRN;

public static class GRNModule
{
    public const string GRNPage = "GRN";

    public static IServiceCollection AddGRNModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<GRNViewModel>(GRNPage)
            .RequireFeature(GRNPage, FeatureFlags.GRN);
        pageRegistry.CachePage(GRNPage);
        services.AddTransient<IGRNService, GRNService>();
        services.AddTransient<GRNViewModel>();
        return services;
    }
}
