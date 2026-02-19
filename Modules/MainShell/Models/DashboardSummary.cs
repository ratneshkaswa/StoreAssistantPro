namespace StoreAssistantPro.Modules.MainShell.Models;

/// <summary>
/// Module-local DTO carrying aggregated dashboard metrics.
/// Keeps DashboardViewModel decoupled from Products and Sales modules.
/// </summary>
public sealed record DashboardSummary(
    int TotalProducts,
    int LowStockCount,
    decimal TodaysSales,
    int TodaysTransactions);
