using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;

namespace StoreAssistantPro.Modules.Products;

public static class ProductsModule
{
    public const string ProductManagementPage = "ProductManagement";
    public const string VariantManagementPage = "VariantManagement";

    public static IServiceCollection AddProductsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<ProductManagementViewModel>(ProductManagementPage)
            .RequireFeature(ProductManagementPage, FeatureFlags.Products);
        pageRegistry.Map<VariantManagementViewModel>(VariantManagementPage)
            .RequireFeature(VariantManagementPage, FeatureFlags.Products);
        services.AddSingleton<ProductContextHolder>();
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<IProductVariantService, ProductVariantService>();
        services.AddTransient<ProductManagementViewModel>();
        services.AddTransient<VariantManagementViewModel>();
        return services;
    }
}
