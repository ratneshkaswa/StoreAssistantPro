using StoreAssistantPro.Modules.MainShell.Models;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Provides aggregated data for the Dashboard view.
/// The implementation may query multiple module services (Products, Sales)
/// but the ViewModel only depends on this single interface, preserving
/// module independence at the ViewModel layer.
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
}
