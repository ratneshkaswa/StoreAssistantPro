using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Settings.Services;

public interface ISystemSettingsService
{
    Task<SystemSettings> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(SystemSettingsDto dto, CancellationToken ct = default);
    Task<string> BackupDatabaseAsync(CancellationToken ct = default);
    Task RestoreDatabaseAsync(string backupPath, CancellationToken ct = default);
    Task FactoryResetAsync(CancellationToken ct = default);
}

public record SystemSettingsDto(
    string? BackupLocation,
    bool AutoBackupEnabled,
    string? BackupTime,
    string? RestoreOption,
    string? DefaultPrinter,
    int PrinterWidth,
    string DefaultPageSize,
    int AutoLogoutMinutes);
