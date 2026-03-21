namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Provides system health metrics for the dashboard (#467).
/// </summary>
public interface ISystemHealthService
{
    /// <summary>Get a snapshot of current system health.</summary>
    Task<SystemHealthSnapshot> GetHealthAsync(CancellationToken ct = default);
}

public record SystemHealthSnapshot(
    bool DatabaseConnected,
    long DatabaseSizeBytes,
    int TotalProducts,
    int TotalSales,
    int PendingReturns,
    double AppMemoryMB,
    TimeSpan Uptime);
