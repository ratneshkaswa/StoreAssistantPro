using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.SystemSettings.Services;

namespace StoreAssistantPro.Modules.SystemSettings.ViewModels;

public partial class BackupSettingsViewModel(
    ISystemSettingsService settingsService,
    IDialogService dialogService) : BaseViewModel
{
    [ObservableProperty]
    public partial string BackupFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [RelayCommand]
    private void Load()
    {
        BackupFolder = settingsService.GetDefaultBackupFolder();
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsRunning = true;

        try
        {
            var filePath = await settingsService.BackupDatabaseAsync(BackupFolder);
            SuccessMessage = $"Backup created: {filePath}";
        }
        catch (NotImplementedException)
        {
            ErrorMessage = "Database backup is not yet configured for this environment.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (!dialogService.Confirm(
            "Restore will overwrite the current database.\n\nThis action cannot be undone. Continue?",
            "Confirm Restore"))
            return;

        IsRunning = true;

        try
        {
            await settingsService.RestoreDatabaseAsync(BackupFolder);
            SuccessMessage = "Database restored successfully.";
        }
        catch (NotImplementedException)
        {
            ErrorMessage = "Database restore is not yet configured for this environment.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = BackupFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
