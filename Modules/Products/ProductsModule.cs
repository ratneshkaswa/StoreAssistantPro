using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.MainShell.Services;
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
        pageRegistry.RequireFeature(ProductsPage, FeatureFlags.Products);

        // Services
        services.AddTransient<IProductService, ProductService>();

        // Command handlers
        services.AddTransient<ICommandRequestHandler<SaveProductCommand, Unit>, SaveProductHandler>();
        services.AddTransient<ICommandRequestHandler<UpdateProductCommand, Unit>, UpdateProductHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteProductCommand, Unit>, DeleteProductHandler>();
        services.AddTransient<ICommandRequestHandler<ImportProductsCommand, ImportProductsResult>, ImportProductsHandler>();
        services.AddTransient<ICommandRequestHandler<BulkDeleteProductsCommand, BulkDeleteProductsResult>, BulkDeleteProductsHandler>();
        services.AddTransient<ICommandRequestHandler<AdjustStockCommand, Unit>, AdjustStockHandler>();

        // Quick actions
        services.AddSingleton<IQuickActionContributor, ProductsQuickActionContributor>();

        // ViewModels
        services.AddTransient<ProductsViewModel>();

        return services;
    }
}
