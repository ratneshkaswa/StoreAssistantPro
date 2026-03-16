using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Salaries.Services;
using StoreAssistantPro.Modules.Salaries.ViewModels;

namespace StoreAssistantPro.Modules.Salaries;

public static class SalariesModule
{
    public const string SalaryManagementPage = "SalaryManagement";

    public static IServiceCollection AddSalariesModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<SalaryManagementViewModel>(SalaryManagementPage)
            .RequireFeature(SalaryManagementPage, FeatureFlags.Salaries);
        services.AddTransient<ISalaryService, SalaryService>();
        services.AddTransient<SalaryManagementViewModel>();
        return services;
    }
}
