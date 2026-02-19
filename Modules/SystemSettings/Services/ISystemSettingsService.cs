namespace StoreAssistantPro.Modules.SystemSettings.Services;

public interface ISystemSettingsService
{
    Task ChangeMasterPinAsync(string currentPin, string newPin);
    Task<bool> CheckDatabaseConnectionAsync();
    Task<string> BackupDatabaseAsync(string backupFolder);
    Task RestoreDatabaseAsync(string backupFilePath);
    string GetDefaultBackupFolder();
}
