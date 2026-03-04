using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Categories.Services;
using StoreAssistantPro.Modules.Categories.ViewModels;
using StoreAssistantPro.Modules.Categories.Views;

namespace StoreAssistantPro.Modules.Categories;

public static class CategoriesModule
{
    public const string CategoryManagementDialog = "CategoryManagement";

    public static IServiceCollection AddCategoriesModule(this IServiceCollection services)
    {
        services.AddTransient<ICategoryService, CategoryService>();
        services.AddTransient<CategoryManagementViewModel>();
        services.AddTransient<CategoryManagementWindow>();
        services.AddDialogRegistration<CategoryManagementWindow>(CategoryManagementDialog);
        return services;
    }
}
