using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Modules.Settings.Services;

namespace StoreAssistantPro.Modules.Settings.ViewModels;

public partial class SystemSettingsViewModel(ISystemSettingsService settingsService) : BaseViewModel
{
    // ── Form fields ──

    [ObservableProperty]
    public partial string BackupLocation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AutoBackupEnabled { get; set; }

    [ObservableProperty]
    public partial string BackupTime { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DefaultPrinter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DefaultTaxMode { get; set; } = "Exclusive";

    public IReadOnlyList<string> TaxModes { get; } = ["Exclusive", "Inclusive"];

    [ObservableProperty]
    public partial string RestoreFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastBackupPath { get; set; } = string.Empty;

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var settings = await settingsService.GetAsync(ct);
        BackupLocation = settings.BackupLocation ?? string.Empty;
        AutoBackupEnabled = settings.AutoBackupEnabled;
        BackupTime = settings.BackupTime ?? string.Empty;
        DefaultPrinter = settings.DefaultPrinter ?? string.Empty;
        DefaultTaxMode = settings.DefaultTaxMode;
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        var dto = new SystemSettingsDto(
            BackupLocation, AutoBackupEnabled, BackupTime,
            null, DefaultPrinter, DefaultTaxMode);

        await settingsService.UpdateAsync(dto, ct);
        SuccessMessage = "Settings saved.";
    });

    [RelayCommand]
    private Task BackupAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;
        var path = await settingsService.BackupDatabaseAsync(ct);
        LastBackupPath = path;
        SuccessMessage = $"Backup saved to {path}";
    });

    [RelayCommand]
    private Task RestoreAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(RestoreFilePath), "Select a backup file first.")))
            return;

        await settingsService.RestoreDatabaseAsync(RestoreFilePath, ct);
        SuccessMessage = "Database restored successfully. Please restart the application.";
    });
}
