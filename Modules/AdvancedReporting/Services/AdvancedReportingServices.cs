using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Reporting;

namespace StoreAssistantPro.Modules.AdvancedReporting.Services;

public sealed class CustomReportService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<CustomReportService> logger) : ICustomReportService
{
    public async Task<IReadOnlyList<CustomReport>> GetReportsAsync(int? userId = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.CustomReports.AsQueryable();
        if (userId.HasValue) query = query.Where(r => r.CreatedByUserId == userId.Value);
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<CustomReport> SaveReportAsync(CustomReport report, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (report.Id == 0) { report.CreatedAt = DateTime.UtcNow; context.CustomReports.Add(report); }
        else context.CustomReports.Update(report);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Custom report saved: {Name}", report.Name);
        return report;
    }

    public async Task DeleteReportAsync(int reportId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var report = await context.CustomReports.FindAsync([reportId], ct).ConfigureAwait(false);
        if (report is null) return;
        context.CustomReports.Remove(report);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleBookmarkAsync(int reportId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var report = await context.CustomReports.FindAsync([reportId], ct).ConfigureAwait(false);
        if (report is null) return;
        report.IsBookmarked = !report.IsBookmarked;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CustomReport>> GetBookmarkedReportsAsync(int userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.CustomReports
            .Where(r => r.CreatedByUserId == userId && r.IsBookmarked)
            .ToListAsync(ct).ConfigureAwait(false);
    }
}

public sealed class ReportScheduleService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<ReportScheduleService> logger) : IReportScheduleService
{
    public async Task<IReadOnlyList<ReportSchedule>> GetSchedulesAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ReportSchedules.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ReportSchedule> SaveScheduleAsync(ReportSchedule schedule, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (schedule.Id == 0) context.ReportSchedules.Add(schedule); else context.ReportSchedules.Update(schedule);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return schedule;
    }

    public async Task DeleteScheduleAsync(int scheduleId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var schedule = await context.ReportSchedules.FindAsync([scheduleId], ct).ConfigureAwait(false);
        if (schedule is null) return;
        context.ReportSchedules.Remove(schedule);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task ProcessDueSchedulesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Processing due report schedules");
        return Task.CompletedTask;
    }
}

public sealed class AnalyticsService(
    IDbContextFactory<AppDbContext> contextFactory) : IAnalyticsService
{
    public async Task<IReadOnlyList<KpiMetric>> GetKpiDashboardAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var today = DateTime.Today;
        var todaySales = await context.Sales.Where(s => s.SaleDate >= today).SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
        var todayCount = await context.Sales.CountAsync(s => s.SaleDate >= today, ct).ConfigureAwait(false);
        var aov = todayCount > 0 ? todaySales / todayCount : 0;

        return
        [
            new("Today's Sales", "Revenue", todaySales, null, "₹", null),
            new("Transactions", "Activity", todayCount, null, null, null),
            new("Average Order Value", "Revenue", aov, null, "₹", null),
            new("Active Products", "Inventory", await context.Products.CountAsync(p => p.IsActive, ct).ConfigureAwait(false), null, null, null),
            new("Low Stock Items", "Inventory", await context.Products.CountAsync(p => p.IsActive && p.MinStockLevel > 0 && p.Quantity <= p.MinStockLevel, ct).ConfigureAwait(false), null, null, "Warning")
        ];
    }

    public async Task<IReadOnlyList<ComparativeEntry>> GetComparativeReportAsync(string metric, DateTime currentFrom, DateTime currentTo, DateTime previousFrom, DateTime previousTo, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var currentSales = await context.Sales.Where(s => s.SaleDate >= currentFrom && s.SaleDate < currentTo)
            .SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
        var prevSales = await context.Sales.Where(s => s.SaleDate >= previousFrom && s.SaleDate < previousTo)
            .SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
        var change = currentSales - prevSales;
        var pct = prevSales > 0 ? (double)(change / prevSales * 100) : 0;

        return [new(metric, currentSales, prevSales, change, pct)];
    }
}

public sealed class ReportAccessService(ILogger<ReportAccessService> logger) : IReportAccessService
{
    private readonly HashSet<(int UserId, int ReportId)> _access = [];

    public Task<bool> CanAccessReportAsync(int userId, int reportId, CancellationToken ct = default)
        => Task.FromResult(_access.Contains((userId, reportId)));

    public Task GrantAccessAsync(int userId, int reportId, CancellationToken ct = default)
    {
        _access.Add((userId, reportId));
        logger.LogInformation("Granted report {ReportId} access to user {UserId}", reportId, userId);
        return Task.CompletedTask;
    }

    public Task RevokeAccessAsync(int userId, int reportId, CancellationToken ct = default)
    {
        _access.Remove((userId, reportId));
        return Task.CompletedTask;
    }
}
