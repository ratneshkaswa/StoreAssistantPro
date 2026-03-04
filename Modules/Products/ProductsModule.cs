using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products;

public static class ProductsModule
{
    public static IServiceCollection AddProductsModule(this IServiceCollection services)
    {
        services.AddTransient<IProductService, ProductService>();
        return services;
    }
}
