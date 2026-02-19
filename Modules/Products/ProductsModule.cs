using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Products.Commands;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;

namespace StoreAssistantPro.Modules.Products;

public static class ProductsModule
{
    public const string ProductsPage = "Products";

    public static IServiceCollection AddProductsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        // Page registration
        pageRegistry.Map<ProductsViewModel>(ProductsPage);

        // Services
        services.AddSingleton<IProductService, ProductService>();

        // Command handlers
        services.AddTransient<ICommandHandler<SaveProductCommand>, SaveProductHandler>();
        services.AddTransient<ICommandHandler<UpdateProductCommand>, UpdateProductHandler>();
        services.AddTransient<ICommandHandler<DeleteProductCommand>, DeleteProductHandler>();

        // ViewModels
        services.AddTransient<ProductsViewModel>();

        return services;
    }
}
