using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Inward.Services;
using StoreAssistantPro.Modules.Inward.ViewModels;

namespace StoreAssistantPro.Modules.Inward;

public static class InwardModule
{
    public const string InwardEntryPage = "InwardEntry";

    public static IServiceCollection AddInwardModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<InwardEntryViewModel>(InwardEntryPage)
            .RequireFeature(InwardEntryPage, FeatureFlags.InwardEntry);
        pageRegistry.CachePage(InwardEntryPage);
        services.AddTransient<IInwardService, InwardService>();
        services.AddTransient<InwardEntryViewModel>();
        return services;
    }
}
