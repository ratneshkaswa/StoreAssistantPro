using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Categories.Services;
using StoreAssistantPro.Modules.Categories.ViewModels;

namespace StoreAssistantPro.Modules.Categories;

public static class CategoriesModule
{
    public const string CategoryManagementPage = "CategoryManagement";

    public static IServiceCollection AddCategoriesModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<CategoryManagementViewModel>(CategoryManagementPage)
            .RequireFeature(CategoryManagementPage, FeatureFlags.Categories);
        services.AddTransient<ICategoryService, CategoryService>();
        services.AddTransient<CategoryManagementViewModel>();
        return services;
    }
}
