using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Budgeting.Services;

namespace StoreAssistantPro.Modules.Budgeting;

public static class BudgetingModule
{
    public static IServiceCollection AddBudgetingModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IBudgetService, BudgetService>();
        services.AddTransient<IBudgetForecastService, BudgetForecastService>();
        services.AddTransient<IFinancialGoalService, FinancialGoalService>();
        return services;
    }
}
