namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Provides system health metrics for the dashboard (#467).
/// </summary>
public interface ISystemHealthService
{
    /// <summary>Get a snapshot of current system health.</summary>
    Task<SystemHealthSnapshot> GetHealthAsync(CancellationToken ct = default);

    /// <summary>Check for a newer app version (#466).</summary>
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default);

    /// <summary>Validate a license key (#465).</summary>
    LicenseStatus ValidateLicense(string? licenseKey);
}

public record SystemHealthSnapshot(
    bool DatabaseConnected,
    long DatabaseSizeBytes,
    int TotalProducts,
    int TotalSales,
    int PendingReturns,
    double AppMemoryMB,
    TimeSpan Uptime);

public record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string Message);

public record LicenseStatus(
    bool IsValid,
    string Tier,
    string Message);
