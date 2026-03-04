using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.FinancialYears.Services;
using StoreAssistantPro.Modules.FinancialYears.ViewModels;
using StoreAssistantPro.Modules.FinancialYears.Views;

namespace StoreAssistantPro.Modules.FinancialYears;

public static class FinancialYearsModule
{
    public const string FinancialYearDialog = "FinancialYear";

    public static IServiceCollection AddFinancialYearsModule(this IServiceCollection services)
    {
        services.AddTransient<IFinancialYearService, FinancialYearService>();
        services.AddTransient<FinancialYearViewModel>();
        services.AddTransient<FinancialYearWindow>();
        services.AddDialogRegistration<FinancialYearWindow>(FinancialYearDialog);
        return services;
    }
}
