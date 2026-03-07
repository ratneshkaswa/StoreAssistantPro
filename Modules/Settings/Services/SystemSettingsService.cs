using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Settings.Services;

public class SystemSettingsService(
    IDbContextFactory<AppDbContext> contextFactory,
    IConfiguration configuration,
    IPerformanceMonitor perf,
    ILogger<SystemSettingsService> logger) : ISystemSettingsService
{
    public async Task<SystemSettings> GetAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SystemSettingsService.GetAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var settings = await context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (settings is not null)
            return settings;

        // Seed default row if none exists
        settings = new SystemSettings
        {
            DefaultTaxMode = "Exclusive",
            AutoBackupEnabled = false
        };
        context.SystemSettings.Add(settings);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return settings;
    }

    public async Task UpdateAsync(SystemSettingsDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("SystemSettingsService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var settings = await context.SystemSettings.FirstOrDefaultAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("System settings not found.");

        settings.BackupLocation = dto.BackupLocation?.Trim();
        settings.AutoBackupEnabled = dto.AutoBackupEnabled;
        settings.BackupTime = dto.BackupTime?.Trim();
        settings.RestoreOption = dto.RestoreOption?.Trim();
        settings.DefaultPrinter = dto.DefaultPrinter?.Trim();
        settings.DefaultTaxMode = string.IsNullOrWhiteSpace(dto.DefaultTaxMode) ? "Exclusive" : dto.DefaultTaxMode.Trim();

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("System settings updated");
    }

    public async Task<string> BackupDatabaseAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SystemSettingsService.BackupDatabaseAsync", TimeSpan.FromSeconds(30));
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var settings = await context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var backupDir = settings?.BackupLocation;
        if (string.IsNullOrWhiteSpace(backupDir))
            backupDir = Path.Combine(AppContext.BaseDirectory, "Backups");

        Directory.CreateDirectory(backupDir);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured.");

        var dbName = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString).InitialCatalog;
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(backupDir, $"{dbName}_{timestamp}.bak");

        // Use parameterized dynamic SQL to prevent SQL injection via file path
        var sql = $"DECLARE @sql NVARCHAR(MAX) = N'BACKUP DATABASE ' + QUOTENAME(@p0) + N' TO DISK = ' + QUOTENAME(@p1, '''') + N' WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD'; EXEC sp_executesql @sql;";

        await context.Database.ExecuteSqlRawAsync(sql, [dbName, backupPath], ct).ConfigureAwait(false);

        logger.LogInformation("Database backed up to {Path}", backupPath);
        return backupPath;
    }

    public async Task RestoreDatabaseAsync(string backupPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);

        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file not found.", backupPath);

        using var _ = perf.BeginScope("SystemSettingsService.RestoreDatabaseAsync", TimeSpan.FromSeconds(60));
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured.");

        var dbName = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString).InitialCatalog;

        // Use parameterized dynamic SQL to prevent SQL injection via file path
        var sql = $"DECLARE @sql NVARCHAR(MAX) = N'ALTER DATABASE ' + QUOTENAME(@p0) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE ' + QUOTENAME(@p0) + N' FROM DISK = ' + QUOTENAME(@p1, '''') + N' WITH REPLACE; ALTER DATABASE ' + QUOTENAME(@p0) + N' SET MULTI_USER;'; EXEC sp_executesql @sql;";

        await context.Database.ExecuteSqlRawAsync(sql, [dbName, backupPath], ct).ConfigureAwait(false);

        logger.LogInformation("Database restored from {Path}", backupPath);
    }

    public async Task<bool> IsSetupCompletedAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var settings = await context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return settings?.SetupCompleted == true;
    }

    public async Task MarkSetupCompletedAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var settings = await context.SystemSettings.FirstOrDefaultAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("System settings not found.");
        settings.SetupCompleted = true;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("First-run setup wizard completed");
    }
}
