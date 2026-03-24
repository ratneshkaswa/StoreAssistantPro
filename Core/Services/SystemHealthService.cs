using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Core.Services;

public class SystemHealthService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : ISystemHealthService
{
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public async Task<SystemHealthSnapshot> GetHealthAsync(CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("SystemHealthService.GetHealthAsync");

        var dbConnected = false;
        long dbSizeBytes = 0;
        var totalProducts = 0;
        var totalSales = 0;
        var pendingReturns = 0;

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            dbConnected = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);

            if (dbConnected)
            {
                totalProducts = await context.Products.CountAsync(ct).ConfigureAwait(false);
                totalSales = await context.Sales.CountAsync(ct).ConfigureAwait(false);
                pendingReturns = await context.SaleReturns
                    .CountAsync(r => !r.StockRestored, ct)
                    .ConfigureAwait(false);

                // Database size via sys.database_files (SQL Server)
                try
                {
                    var sizeResult = await context.Database
                        .SqlQueryRaw<long>("SELECT CAST(SUM(size) * 8192 AS BIGINT) AS [Value] FROM sys.database_files")
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false);
                    dbSizeBytes = sizeResult;
                }
                catch
                {
                    // Non-critical — size query may fail on limited permissions
                }
            }
        }
        catch
        {
            dbConnected = false;
        }

        var process = Process.GetCurrentProcess();
        var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);
        var uptime = DateTime.UtcNow - _startTime;

        return new SystemHealthSnapshot(
            dbConnected,
            dbSizeBytes,
            totalProducts,
            totalSales,
            pendingReturns,
            Math.Round(memoryMB, 1),
            uptime);
    }

    public Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default)
    {
        // Stub: in production, this would check a remote API for the latest version.
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        var currentVersion = assembly?.GetName().Version?.ToString() ?? "1.0.0.0";
        return Task.FromResult(new UpdateCheckResult(
            false,
            currentVersion,
            null,
            "You are running the latest version."));
    }

    public LicenseStatus ValidateLicense(string? licenseKey)
    {
        // Stub: in production, this would validate against a license server.
        if (string.IsNullOrWhiteSpace(licenseKey))
            return new LicenseStatus(true, "Community", "Community edition — no license required.");

        // Basic format check: expect format like "SAPRO-XXXX-XXXX-XXXX"
        if (licenseKey.StartsWith("SAPRO-", StringComparison.OrdinalIgnoreCase) && licenseKey.Length >= 20)
            return new LicenseStatus(true, "Pro", "License validated successfully.");

        return new LicenseStatus(false, "Invalid", "License key format is not recognized.");
    }
}
