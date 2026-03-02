using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.Models;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Provides a minimal <see cref="DashboardSummary"/> for the status bar.
/// </summary>
public class DashboardService(IPerformanceMonitor perf) : IDashboardService
{
    public Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DashboardService.GetSummaryAsync");
        return Task.FromResult(DashboardSummary.Empty);
    }
}
