using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Modules.Settings.Services;

namespace StoreAssistantPro.Modules.Settings.ViewModels;

public partial class SystemSettingsViewModel(ISystemSettingsService settingsService) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackupModeTitle))]
    public partial string BackupLocation { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackupScheduleSummary))]
    [NotifyPropertyChangedFor(nameof(BackupModeTitle))]
    public partial bool AutoBackupEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackupScheduleSummary))]
    [NotifyPropertyChangedFor(nameof(BackupModeTitle))]
    public partial string BackupTime { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrinterSummaryText))]
    public partial string DefaultPrinter { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RestoreStatusText))]
    public partial string RestoreFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastBackupSummary))]
    public partial string LastBackupPath { get; set; } = string.Empty;

    public string BackupScheduleSummary => AutoBackupEnabled
        ? string.IsNullOrWhiteSpace(BackupTime)
            ? "Auto backup is enabled. Choose a daily backup time."
            : $"Auto backup runs at {BackupTime.Trim()}."
        : "Auto backup is currently disabled.";

    public string BackupModeTitle => AutoBackupEnabled
        ? string.IsNullOrWhiteSpace(BackupTime)
            ? "Auto backup needs a time"
            : $"Daily backup at {BackupTime.Trim()}"
        : "Manual backups only";

    public string PrinterSummaryText => string.IsNullOrWhiteSpace(DefaultPrinter)
        ? "System default printer"
        : DefaultPrinter.Trim();

    public string RestoreStatusText => string.IsNullOrWhiteSpace(RestoreFilePath)
        ? "No restore file selected."
        : $"Restore source: {Path.GetFileName(RestoreFilePath.Trim())}";

    public string LastBackupSummary => string.IsNullOrWhiteSpace(LastBackupPath)
        ? "No backup has been created in this session."
        : $"Last backup: {Path.GetFileName(LastBackupPath.Trim())}";

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var settings = await settingsService.GetAsync(ct).ConfigureAwait(false);
        BackupLocation = settings.BackupLocation ?? string.Empty;
        AutoBackupEnabled = settings.AutoBackupEnabled;
        BackupTime = settings.BackupTime ?? string.Empty;
        DefaultPrinter = settings.DefaultPrinter ?? string.Empty;
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        var isValid = Validate(v => v
            .Rule(!AutoBackupEnabled || !string.IsNullOrWhiteSpace(BackupTime), "Backup time is required when auto backup is enabled.", nameof(BackupTime))
            .Rule(!AutoBackupEnabled || TimeOnly.TryParseExact(BackupTime.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _), "Backup time must use HH:mm format.", nameof(BackupTime)));

        if (!isValid)
            return;

        var dto = new SystemSettingsDto(
            BackupLocation,
            AutoBackupEnabled,
            BackupTime,
            null,
            DefaultPrinter);

        await settingsService.UpdateAsync(dto, ct).ConfigureAwait(false);
        SuccessMessage = "Settings saved.";
    });

    [RelayCommand]
    private Task BackupAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;
        var path = await settingsService.BackupDatabaseAsync(ct).ConfigureAwait(false);
        LastBackupPath = path;
        SuccessMessage = $"Backup saved to {path}";
    });

    [RelayCommand]
    private Task RestoreAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(RestoreFilePath), "Select a backup file first.", nameof(RestoreFilePath))))
            return;

        await settingsService.RestoreDatabaseAsync(RestoreFilePath, ct).ConfigureAwait(false);
        SuccessMessage = "Database restored successfully. Please restart the application.";
    });
}
