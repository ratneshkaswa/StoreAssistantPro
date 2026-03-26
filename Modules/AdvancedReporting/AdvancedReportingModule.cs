using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.AdvancedReporting.Services;

namespace StoreAssistantPro.Modules.AdvancedReporting;

public static class AdvancedReportingModule
{
    public static IServiceCollection AddAdvancedReportingModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<ICustomReportService, CustomReportService>();
        services.AddTransient<IReportScheduleService, ReportScheduleService>();
        services.AddTransient<IAnalyticsService, AnalyticsService>();
        // Report access holds in-memory access sets → Singleton.
        services.AddSingleton<IReportAccessService, ReportAccessService>();
        return services;
    }
}
