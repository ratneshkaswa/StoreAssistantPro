using System.IO;
using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Modules.SystemSettings.Services;

public class SystemSettingsService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILoginService loginService) : ISystemSettingsService
{
    public async Task ChangeMasterPinAsync(string currentPin, string newPin)
    {
        var isValid = await loginService.ValidateMasterPinAsync(currentPin);
        if (!isValid)
            throw new InvalidOperationException("Current Master Password is incorrect.");

        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Application configuration not found.");

        config.MasterPinHash = PinHasher.Hash(newPin);
        await context.SaveChangesAsync();
    }

    public async Task<bool> CheckDatabaseConnectionAsync()
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    public Task<string> BackupDatabaseAsync(string backupFolder)
    {
        // Architecture placeholder — implementation depends on SQL Server backup strategy.
        // Production: execute BACKUP DATABASE via raw SQL or use SqlClient directly.
        var fileName = $"StoreAssistantPro_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var filePath = Path.Combine(backupFolder, fileName);

        // TODO: Execute SQL Server BACKUP DATABASE command
        throw new NotImplementedException(
            "Database backup requires SQL Server BACKUP DATABASE permissions. " +
            "Configure backup strategy in production deployment.");
    }

    public Task RestoreDatabaseAsync(string backupFilePath)
    {
        // Architecture placeholder — implementation depends on SQL Server restore strategy.
        // Production: execute RESTORE DATABASE via raw SQL or use SqlClient directly.

        // TODO: Execute SQL Server RESTORE DATABASE command
        throw new NotImplementedException(
            "Database restore requires SQL Server RESTORE DATABASE permissions. " +
            "Configure restore strategy in production deployment.");
    }

    public string GetDefaultBackupFolder()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StoreAssistantPro", "Backups");

        Directory.CreateDirectory(folder);
        return folder;
    }
}
