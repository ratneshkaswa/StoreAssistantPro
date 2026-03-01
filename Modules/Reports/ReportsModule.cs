using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Reports.Services;

namespace StoreAssistantPro.Modules.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddSingleton<IReportingService, ReportingService>();

        return services;
    }
}
