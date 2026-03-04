using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Brands.Services;
using StoreAssistantPro.Modules.Brands.ViewModels;
using StoreAssistantPro.Modules.Brands.Views;

namespace StoreAssistantPro.Modules.Brands;

public static class BrandsModule
{
    public const string BrandManagementDialog = "BrandManagement";

    public static IServiceCollection AddBrandsModule(this IServiceCollection services)
    {
        services.AddTransient<IBrandService, BrandService>();
        services.AddTransient<BrandManagementViewModel>();
        services.AddTransient<BrandManagementWindow>();
        services.AddDialogRegistration<BrandManagementWindow>(BrandManagementDialog);
        return services;
    }
}
