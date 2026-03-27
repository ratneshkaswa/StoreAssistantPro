using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Printing;
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
        pageRegistry.Map<ReportsViewModel>(ReportsPage)
            .RequireFeature(ReportsPage, FeatureFlags.Reports)
            .CachePage(ReportsPage);
        services.AddTransient<IReportsService, ReportsService>();
        services.AddTransient<IPrintReportService, PrintReportService>();
        services.AddTransient<IPrintPreviewService, PrintPreviewService>();
        services.AddTransient<ReportsViewModel>();
        return services;
    }
}
