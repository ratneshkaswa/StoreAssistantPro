using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.SalesPurchase.Services;
using StoreAssistantPro.Modules.SalesPurchase.ViewModels;

namespace StoreAssistantPro.Modules.SalesPurchase;

public static class SalesPurchaseModule
{
    public const string SalesPurchasePage = "SalesPurchase";

    public static IServiceCollection AddSalesPurchaseModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<SalesPurchaseViewModel>(SalesPurchasePage)
            .RequireFeature(SalesPurchasePage, FeatureFlags.SalesPurchase);
        services.AddTransient<ISalesPurchaseService, SalesPurchaseService>();
        services.AddTransient<SalesPurchaseViewModel>();
        return services;
    }
}
