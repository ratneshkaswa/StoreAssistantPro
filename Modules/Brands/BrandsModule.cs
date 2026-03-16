using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Brands.Services;
using StoreAssistantPro.Modules.Brands.ViewModels;

namespace StoreAssistantPro.Modules.Brands;

public static class BrandsModule
{
    public const string BrandManagementPage = "BrandManagement";

    public static IServiceCollection AddBrandsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<BrandManagementViewModel>(BrandManagementPage)
            .RequireFeature(BrandManagementPage, FeatureFlags.Brands);
        services.AddTransient<IBrandService, BrandService>();
        services.AddTransient<BrandManagementViewModel>();
        return services;
    }
}
