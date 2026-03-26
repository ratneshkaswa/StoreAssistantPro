using StoreAssistantPro.Models.Reporting;

namespace StoreAssistantPro.Modules.AdvancedReporting.Services;

/// <summary>Custom report builder service (#940, #942).</summary>
public interface ICustomReportService
{
    Task<IReadOnlyList<CustomReport>> GetReportsAsync(int? userId = null, CancellationToken ct = default);
    Task<CustomReport> SaveReportAsync(CustomReport report, CancellationToken ct = default);
    Task DeleteReportAsync(int reportId, CancellationToken ct = default);
    Task ToggleBookmarkAsync(int reportId, CancellationToken ct = default);
    Task<IReadOnlyList<CustomReport>> GetBookmarkedReportsAsync(int userId, CancellationToken ct = default);
}

/// <summary>Report scheduling service (#941).</summary>
public interface IReportScheduleService
{
    Task<IReadOnlyList<ReportSchedule>> GetSchedulesAsync(CancellationToken ct = default);
    Task<ReportSchedule> SaveScheduleAsync(ReportSchedule schedule, CancellationToken ct = default);
    Task DeleteScheduleAsync(int scheduleId, CancellationToken ct = default);
    Task ProcessDueSchedulesAsync(CancellationToken ct = default);
}

/// <summary>KPI and comparative analytics service (#943-947).</summary>
public interface IAnalyticsService
{
    Task<IReadOnlyList<KpiMetric>> GetKpiDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ComparativeEntry>> GetComparativeReportAsync(string metric, DateTime currentFrom, DateTime currentTo, DateTime previousFrom, DateTime previousTo, CancellationToken ct = default);
}

/// <summary>Report access control service (#948).</summary>
public interface IReportAccessService
{
    Task<bool> CanAccessReportAsync(int userId, int reportId, CancellationToken ct = default);
    Task GrantAccessAsync(int userId, int reportId, CancellationToken ct = default);
    Task RevokeAccessAsync(int userId, int reportId, CancellationToken ct = default);
}
