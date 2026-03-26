using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Settings.Services;
using StoreAssistantPro.Modules.Settings.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public class SystemSettingsViewModelTests : IDisposable
{
    private readonly ISystemSettingsService _settingsService = Substitute.For<ISystemSettingsService>();
    private readonly ILoginService _loginService = Substitute.For<ILoginService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IUiDensityService _uiDensityService = Substitute.For<IUiDensityService>();

    public SystemSettingsViewModelTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    private SystemSettingsViewModel CreateSut() => new(_settingsService, _loginService, _dialogService, _uiDensityService);

    [Fact]
    public async Task LoadCommand_SyncsNumericBridgeProperties()
    {
        _settingsService.GetAsync(Arg.Any<CancellationToken>()).Returns(new SystemSettings
        {
            DefaultPrinter = "Counter Printer",
            PrinterWidth = 58,
            DefaultPageSize = "Thermal",
            AutoLogoutMinutes = 15
        });

        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal(58, sut.PrinterWidth);
        Assert.Equal(58d, sut.PrinterWidthValue);
        Assert.Equal("Thermal", sut.DefaultPageSize);
        Assert.Equal(15, sut.AutoLogoutMinutes);
        Assert.Equal(15d, sut.AutoLogoutMinutesValue);
    }

    [Fact]
    public void NumericBridgeProperties_RoundAndClamp_ToBackedIntegers()
    {
        var sut = CreateSut();

        sut.PrinterWidthValue = 79.6;
        sut.AutoLogoutMinutesValue = -5;

        Assert.Equal(80, sut.PrinterWidth);
        Assert.Equal(80d, sut.PrinterWidthValue);
        Assert.Equal(0, sut.AutoLogoutMinutes);
        Assert.Equal(0d, sut.AutoLogoutMinutesValue);
        Assert.Equal("Auto-logout is disabled.", sut.AutoLogoutSummary);
    }

    [Fact]
    public void SetDensityModeCommand_UpdatesUiDensityService()
    {
        var sut = CreateSut();

        sut.SetDensityModeCommand.Execute("Compact");

        Assert.True(sut.IsCompactModeEnabled);
        _uiDensityService.Received(1).SetCompactMode(true);
    }

    [Fact]
    public async Task LoadCommand_Should_Read_Experience_And_Notification_Preferences()
    {
        UserPreferencesStore.Update(state =>
        {
            state.RestoreLastVisitedPageOnLogin = false;
            state.InAppToastsEnabled = false;
            state.WindowsNotificationsEnabled = true;
            state.NotificationSoundEnabled = true;
            state.MinimumNotificationLevel = AppNotificationLevel.Warning;
        });

        _settingsService.GetAsync(Arg.Any<CancellationToken>()).Returns(new SystemSettings());
        _uiDensityService.IsCompactModeEnabled.Returns(true);
        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.False(sut.RestoreLastVisitedPageOnLogin);
        Assert.False(sut.InAppToastsEnabled);
        Assert.True(sut.WindowsNotificationsEnabled);
        Assert.True(sut.NotificationSoundEnabled);
        Assert.Equal(AppNotificationLevel.Warning, sut.MinimumNotificationLevel);
    }

    [Fact]
    public async Task SaveCommand_Should_Persist_Experience_And_Notification_Preferences()
    {
        _settingsService.UpdateAsync(Arg.Any<SystemSettingsDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.BackupTime = "09:30";
        sut.AutoBackupEnabled = false;
        sut.RestoreLastVisitedPageOnLogin = false;
        sut.InAppToastsEnabled = false;
        sut.WindowsNotificationsEnabled = true;
        sut.NotificationSoundEnabled = true;
        sut.MinimumNotificationLevel = AppNotificationLevel.Error;

        await sut.SaveCommand.ExecuteAsync(null);

        var snapshot = UserPreferencesStore.GetSnapshot();
        Assert.False(snapshot.RestoreLastVisitedPageOnLogin);
        Assert.False(snapshot.InAppToastsEnabled);
        Assert.True(snapshot.WindowsNotificationsEnabled);
        Assert.True(snapshot.NotificationSoundEnabled);
        Assert.Equal(AppNotificationLevel.Error, snapshot.MinimumNotificationLevel);
        Assert.False(sut.IsDirty);
    }

    [Fact]
    public async Task RestoreCommand_WhenCancelled_ShouldNotRestoreDatabase()
    {
        _dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var sut = CreateSut();
        sut.RestoreFilePath = @"C:\Backups\store-20260326.bak";

        await sut.RestoreCommand.ExecuteAsync(null);

        _dialogService.Received(1).Confirm(
            Arg.Is<string>(message => message.Contains("store-20260326.bak", StringComparison.Ordinal)),
            "Restore Database",
            "Restore Database",
            "Cancel");
        await _settingsService.DidNotReceive().RestoreDatabaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FactoryResetCommand_ShouldUseExplicitDestructiveLabels()
    {
        _dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var sut = CreateSut();

        await sut.FactoryResetCommand.ExecuteAsync(null);

        _dialogService.Received(1).Confirm(
            Arg.Is<string>(message => message.Contains("factory defaults", StringComparison.Ordinal)),
            "Factory Reset",
            "Factory Reset",
            "Cancel");
        _dialogService.DidNotReceive().PromptPassword(Arg.Any<string>(), Arg.Any<string>());
        await _settingsService.DidNotReceive().FactoryResetAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditableSettings_Should_TrackDirtyState_AfterLoad_And_ResetOnSave()
    {
        _settingsService.GetAsync(Arg.Any<CancellationToken>()).Returns(new SystemSettings
        {
            DefaultPrinter = "Counter Printer",
            PrinterWidth = 58,
            DefaultPageSize = "Thermal",
            AutoLogoutMinutes = 15
        });
        _settingsService.UpdateAsync(Arg.Any<SystemSettingsDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.False(sut.IsDirty);

        sut.DefaultPrinter = "Back Office Printer";

        Assert.True(sut.IsDirty);
        Assert.Equal("You have unsaved workstation settings changes.", sut.DirtyStateSummaryText);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.False(sut.IsDirty);
    }

    [Fact]
    public async Task InvalidBackupTime_ShouldPopulateValidationSummary()
    {
        var sut = CreateSut();
        sut.AutoBackupEnabled = true;
        sut.BackupTime = "bad";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.HasValidationErrors);
        Assert.Contains("Backup time must use HH:mm format.", sut.ValidationErrors);
        Assert.Equal(nameof(SystemSettingsViewModel.BackupTime), sut.FirstErrorFieldKey);
    }
}
