using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Modules.Tax.ViewModels;

namespace StoreAssistantPro.Modules.Tax;

public static class TaxModule
{
    public const string TaxManagementPage = "TaxManagement";

    public static IServiceCollection AddTaxModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<TaxManagementViewModel>(TaxManagementPage)
            .RequireFeature(TaxManagementPage, FeatureFlags.TaxManagement);
        pageRegistry.CachePage(TaxManagementPage);
        services.AddTransient<ITaxService, TaxService>();
        services.AddTransient<ITaxGroupService, TaxGroupService>();
        services.AddTransient<TaxManagementViewModel>();
        return services;
    }
}
