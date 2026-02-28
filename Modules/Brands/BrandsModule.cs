using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Brands.Commands;
using StoreAssistantPro.Modules.Brands.Services;
using StoreAssistantPro.Modules.Brands.ViewModels;

namespace StoreAssistantPro.Modules.Brands;

public static class BrandsModule
{
    public const string BrandsPage = "Brands";

    public static IServiceCollection AddBrandsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        // Page registration
        pageRegistry.Map<BrandsViewModel>(BrandsPage);

        // Services
        services.AddTransient<IBrandService, BrandService>();

        // Command handlers
        services.AddTransient<ICommandRequestHandler<SaveBrandCommand, Unit>, SaveBrandHandler>();
        services.AddTransient<ICommandRequestHandler<UpdateBrandCommand, Unit>, UpdateBrandHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteBrandCommand, Unit>, DeleteBrandHandler>();

        // ViewModels
        services.AddTransient<BrandsViewModel>();

        return services;
    }
}
