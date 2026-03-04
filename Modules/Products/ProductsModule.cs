using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;
using StoreAssistantPro.Modules.Products.Views;

namespace StoreAssistantPro.Modules.Products;

public static class ProductsModule
{
    public const string ProductManagementDialog = "ProductManagement";

    public static IServiceCollection AddProductsModule(this IServiceCollection services)
    {
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<ProductManagementViewModel>();
        services.AddTransient<ProductManagementWindow>();
        services.AddDialogRegistration<ProductManagementWindow>(ProductManagementDialog);
        return services;
    }
}
