using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.FinancialYears.Services;

namespace StoreAssistantPro.Modules.FinancialYears;

public static class FinancialYearsModule
{
    public static IServiceCollection AddFinancialYearsModule(this IServiceCollection services)
    {
        services.AddSingleton<IFinancialYearService, FinancialYearService>();

        return services;
    }
}
