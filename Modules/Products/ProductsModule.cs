using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;
using StoreAssistantPro.Modules.Products.Views;

namespace StoreAssistantPro.Modules.Products;

public static class ProductsModule
{
    public const string ProductManagementDialog = "ProductManagement";
    public const string VariantManagementDialog = "VariantManagement";

    public static IServiceCollection AddProductsModule(this IServiceCollection services)
    {
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<IProductVariantService, ProductVariantService>();
        services.AddTransient<ProductManagementViewModel>();
        services.AddTransient<VariantManagementViewModel>();
        services.AddTransient<ProductManagementWindow>();
        services.AddTransient<VariantManagementWindow>();
        services.AddDialogRegistration<ProductManagementWindow>(ProductManagementDialog);
        services.AddDialogRegistration<VariantManagementWindow>(VariantManagementDialog);
        return services;
    }
}
