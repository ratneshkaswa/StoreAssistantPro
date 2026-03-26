using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Settings.Services;

namespace StoreAssistantPro.Modules.Settings.ViewModels;

public partial class SystemSettingsViewModel(
    ISystemSettingsService settingsService,
    ILoginService loginService,
    IDialogService dialogService,
    IUiDensityService uiDensityService) : BaseViewModel
{
    private bool _isHydrating;

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
    [NotifyPropertyChangedFor(nameof(PrinterSummaryText))]
    public partial int PrinterWidth { get; set; }

    [ObservableProperty]
    public partial double PrinterWidthValue { get; set; }

    [ObservableProperty]
    public partial string DefaultPageSize { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutoLogoutSummary))]
    public partial int AutoLogoutMinutes { get; set; }

    [ObservableProperty]
    public partial double AutoLogoutMinutesValue { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DensitySummaryText))]
    public partial bool IsCompactModeEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceRestoreSummaryText))]
    public partial bool RestoreLastVisitedPageOnLogin { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotificationPreferencesSummaryText))]
    public partial bool InAppToastsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotificationPreferencesSummaryText))]
    public partial bool WindowsNotificationsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotificationPreferencesSummaryText))]
    public partial bool NotificationSoundEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotificationPreferencesSummaryText))]
    public partial AppNotificationLevel MinimumNotificationLevel { get; set; } = AppNotificationLevel.Info;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RestoreStatusText))]
    public partial string RestoreFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastBackupSummary))]
    public partial string LastBackupPath { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DirtyStateSummaryText))]
    public partial bool IsDirty { get; set; }

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
        : $"{DefaultPrinter.Trim()} ({(PrinterWidth > 0 ? $"{PrinterWidth}mm" : "auto")})";

    public string AutoLogoutSummary => AutoLogoutMinutes > 0
        ? $"Auto-logout after {AutoLogoutMinutes} minutes of inactivity."
        : "Auto-logout is disabled.";

    public string DensitySummaryText => IsCompactModeEnabled
        ? "Compact density is active. Shared table rows use the tighter layout."
        : "Normal density is active. Shared table rows use the standard layout.";

    public string WorkspaceRestoreSummaryText => RestoreLastVisitedPageOnLogin
        ? "Reopen the last visited page after the workstation signs back in."
        : "Always start from Home after the workstation signs back in.";

    public string NotificationPreferencesSummaryText
    {
        get
        {
            var surfaces = new List<string>();
            if (InAppToastsEnabled)
                surfaces.Add("in-app toasts");
            if (WindowsNotificationsEnabled)
                surfaces.Add("Windows notifications");

            var surfaceSummary = surfaces.Count switch
            {
                0 => "Notifications are muted.",
                1 => $"Using {surfaces[0]} only.",
                _ => "Using in-app toasts and Windows notifications."
            };

            var soundSummary = NotificationSoundEnabled ? "Sound is on." : "Sound is off.";
            var thresholdSummary = MinimumNotificationLevel switch
            {
                AppNotificationLevel.Error => "Only errors break through.",
                AppNotificationLevel.Warning => "Warnings and errors break through.",
                _ => "Info, success, warnings, and errors break through."
            };

            return $"{surfaceSummary} {soundSummary} {thresholdSummary}";
        }
    }

    public string RestoreStatusText => string.IsNullOrWhiteSpace(RestoreFilePath)
        ? "No restore file selected."
        : $"Restore source: {Path.GetFileName(RestoreFilePath.Trim())}";

    public string LastBackupSummary => string.IsNullOrWhiteSpace(LastBackupPath)
        ? "No backup has been created in this session."
        : $"Last backup: {Path.GetFileName(LastBackupPath.Trim())}";

    public string DirtyStateSummaryText => IsDirty
        ? "You have unsaved workstation settings changes."
        : "No unsaved workstation settings changes.";

    partial void OnBackupLocationChanged(string value) => MarkDirtyFromEditableChange();
    partial void OnAutoBackupEnabledChanged(bool value) => MarkDirtyFromEditableChange();
    partial void OnBackupTimeChanged(string value) => MarkDirtyFromEditableChange();
    partial void OnDefaultPrinterChanged(string value) => MarkDirtyFromEditableChange();

    partial void OnPrinterWidthChanged(int value)
    {
        if (Math.Abs(PrinterWidthValue - value) > double.Epsilon)
            PrinterWidthValue = value;

        MarkDirtyFromEditableChange();
    }

    partial void OnDefaultPageSizeChanged(string value) => MarkDirtyFromEditableChange();

    partial void OnPrinterWidthValueChanged(double value)
    {
        var normalizedValue = Math.Max(0d, Math.Round(value, MidpointRounding.AwayFromZero));
        if (Math.Abs(value - normalizedValue) > double.Epsilon)
        {
            if (Math.Abs(PrinterWidthValue - normalizedValue) > double.Epsilon)
                PrinterWidthValue = normalizedValue;
            return;
        }

        var roundedValue = (int)normalizedValue;
        if (PrinterWidth != roundedValue)
            PrinterWidth = roundedValue;
    }

    partial void OnAutoLogoutMinutesChanged(int value)
    {
        if (Math.Abs(AutoLogoutMinutesValue - value) > double.Epsilon)
            AutoLogoutMinutesValue = value;

        MarkDirtyFromEditableChange();
    }

    partial void OnAutoLogoutMinutesValueChanged(double value)
    {
        var normalizedValue = Math.Max(0d, Math.Round(value, MidpointRounding.AwayFromZero));
        if (Math.Abs(value - normalizedValue) > double.Epsilon)
        {
            if (Math.Abs(AutoLogoutMinutesValue - normalizedValue) > double.Epsilon)
                AutoLogoutMinutesValue = normalizedValue;
            return;
        }

        var roundedValue = (int)normalizedValue;
        if (AutoLogoutMinutes != roundedValue)
            AutoLogoutMinutes = roundedValue;
    }

    partial void OnIsCompactModeEnabledChanged(bool value)
    {
        uiDensityService.SetCompactMode(value);
        MarkDirtyFromEditableChange();
    }

    partial void OnRestoreLastVisitedPageOnLoginChanged(bool value) => MarkDirtyFromEditableChange();
    partial void OnInAppToastsEnabledChanged(bool value) => MarkDirtyFromEditableChange();
    partial void OnWindowsNotificationsEnabledChanged(bool value) => MarkDirtyFromEditableChange();
    partial void OnNotificationSoundEnabledChanged(bool value) => MarkDirtyFromEditableChange();
    partial void OnMinimumNotificationLevelChanged(AppNotificationLevel value) => MarkDirtyFromEditableChange();

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        _isHydrating = true;
        try
        {
            var settings = await settingsService.GetAsync(ct).ConfigureAwait(false);
            BackupLocation = settings.BackupLocation ?? string.Empty;
            AutoBackupEnabled = settings.AutoBackupEnabled;
            BackupTime = settings.BackupTime ?? string.Empty;
            DefaultPrinter = settings.DefaultPrinter ?? string.Empty;
            PrinterWidth = settings.PrinterWidth;
            DefaultPageSize = settings.DefaultPageSize;
            AutoLogoutMinutes = settings.AutoLogoutMinutes;
            IsCompactModeEnabled = uiDensityService.IsCompactModeEnabled;
            var preferences = UserPreferencesStore.GetSnapshot();
            RestoreLastVisitedPageOnLogin = preferences.RestoreLastVisitedPageOnLogin;
            InAppToastsEnabled = preferences.InAppToastsEnabled;
            WindowsNotificationsEnabled = preferences.WindowsNotificationsEnabled;
            NotificationSoundEnabled = preferences.NotificationSoundEnabled;
            MinimumNotificationLevel = preferences.MinimumNotificationLevel;
        }
        finally
        {
            _isHydrating = false;
        }

        IsDirty = false;
        ValidationErrors = [];
    });

    [RelayCommand]
    private void SetDensityMode(string? mode) =>
        IsCompactModeEnabled = string.Equals(mode, "Compact", StringComparison.OrdinalIgnoreCase);

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
            DefaultPrinter,
            PrinterWidth,
            DefaultPageSize,
            AutoLogoutMinutes);

        await settingsService.UpdateAsync(dto, ct).ConfigureAwait(false);
        UserPreferencesStore.Update(state =>
        {
            state.RestoreLastVisitedPageOnLogin = RestoreLastVisitedPageOnLogin;
            state.InAppToastsEnabled = InAppToastsEnabled;
            state.WindowsNotificationsEnabled = WindowsNotificationsEnabled;
            state.NotificationSoundEnabled = NotificationSoundEnabled;
            state.MinimumNotificationLevel = MinimumNotificationLevel;
        });
        IsDirty = false;
        ValidationErrors = [];
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

        if (!dialogService.Confirm(
                $"Restore the database from '{Path.GetFileName(RestoreFilePath)}'?\n\nThis replaces all current workstation data and cannot be undone.",
                "Restore Database",
                "Restore Database",
                "Cancel"))
            return;

        await settingsService.RestoreDatabaseAsync(RestoreFilePath, ct).ConfigureAwait(false);
        SuccessMessage = "Database restored successfully. Please restart the application.";
    });

    [RelayCommand]
    private Task FactoryResetAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!dialogService.Confirm(
            "This will permanently delete ALL data and reset the application to factory defaults.\n\nAre you sure?",
            "Factory Reset",
            "Factory Reset",
            "Cancel"))
            return;

        var masterPin = dialogService.PromptPassword(
            "Enter the Master PIN to confirm factory reset.",
            "Master PIN Required");

        if (string.IsNullOrWhiteSpace(masterPin))
        {
            ErrorMessage = "Factory reset cancelled.";
            return;
        }

        var isValid = await loginService.ValidateMasterPinAsync(masterPin, ct).ConfigureAwait(false);
        if (!isValid)
        {
            ErrorMessage = "Invalid Master PIN. Factory reset aborted.";
            return;
        }

        await settingsService.FactoryResetAsync(ct).ConfigureAwait(false);
        SuccessMessage = "Factory reset complete. Please restart the application.";
    });

    private void MarkDirtyFromEditableChange()
    {
        if (_isHydrating)
            return;

        IsDirty = true;

        if (!string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;

        if (!string.IsNullOrEmpty(SuccessMessage))
            SuccessMessage = string.Empty;

        if (!string.IsNullOrEmpty(FirstErrorFieldKey))
            FirstErrorFieldKey = string.Empty;

        ValidationErrors = [];
    }
}
