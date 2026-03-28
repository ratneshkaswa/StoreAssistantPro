using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.BarcodeLabels.Services;
using StoreAssistantPro.Modules.BarcodeLabels.ViewModels;

namespace StoreAssistantPro.Modules.BarcodeLabels;

public static class BarcodeLabelsModule
{
    public const string BarcodeLabelsPage = "BarcodeLabels";

    public static IServiceCollection AddBarcodeLabelsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<BarcodeLabelViewModel>(BarcodeLabelsPage)
            .RequireFeature(BarcodeLabelsPage, FeatureFlags.Products);
        pageRegistry.CachePage(BarcodeLabelsPage);
        services.AddTransient<IBarcodeLabelService, BarcodeLabelService>();
        services.AddTransient<BarcodeLabelViewModel>();
        return services;
    }
}
