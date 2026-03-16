using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.FinancialYears.Services;
using StoreAssistantPro.Modules.FinancialYears.ViewModels;

namespace StoreAssistantPro.Modules.FinancialYears;

public static class FinancialYearsModule
{
    public const string FinancialYearPage = "FinancialYear";

    public static IServiceCollection AddFinancialYearsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<FinancialYearViewModel>(FinancialYearPage)
            .RequireFeature(FinancialYearPage, FeatureFlags.FinancialYear);
        services.AddTransient<IFinancialYearService, FinancialYearService>();
        services.AddTransient<FinancialYearViewModel>();
        return services;
    }
}
