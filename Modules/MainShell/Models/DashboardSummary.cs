namespace StoreAssistantPro.Modules.MainShell.Models;

/// <summary>
/// Module-local DTO carrying aggregated dashboard metrics.
/// </summary>
public sealed record DashboardSummary
{
    public static readonly DashboardSummary Empty = new();
}
