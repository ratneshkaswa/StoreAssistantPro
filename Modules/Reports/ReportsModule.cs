using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Reports.Services;
using StoreAssistantPro.Modules.Reports.ViewModels;

namespace StoreAssistantPro.Modules.Reports;

public static class ReportsModule
{
    public const string ReportsPage = "Reports";

    public static IServiceCollection AddReportsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<ReportsViewModel>(ReportsPage);

        services.AddSingleton<IReportingService, ReportingService>();
        services.AddTransient<ReportsViewModel>();

        return services;
    }
}
