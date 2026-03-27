using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Backup.Services;
using StoreAssistantPro.Modules.Backup.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class BackupRestoreViewModelTests
{
    private readonly IBackupService _backupService = Substitute.For<IBackupService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private BackupRestoreViewModel CreateSut()
    {
        _backupService.GetDefaultBackupFolder().Returns(@"C:\Backups");
        _appState.CurrentUserType.Returns(UserType.Admin);
        return new BackupRestoreViewModel(_backupService, _auditService, _appState, _dialogService, _eventBus);
    }

    [Fact]
    public async Task RequestRestoreCommand_WhenNoBackupSelected_ShouldShowError()
    {
        var sut = CreateSut();

        await sut.RequestRestoreCommand.ExecuteAsync(null);

        Assert.Equal("Select a backup file to restore.", sut.ErrorMessage);
        _dialogService.DidNotReceive().Confirm(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _backupService.DidNotReceive().RestoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestRestoreCommand_WhenCancelled_ShouldNotRestore()
    {
        _dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var sut = CreateSut();
        sut.SelectedBackup = new BackupFileInfo("store-20260326.bak", @"C:\Backups\store-20260326.bak", DateTime.Today, 2048);

        await sut.RequestRestoreCommand.ExecuteAsync(null);

        _dialogService.Received(1).Confirm(
            Arg.Is<string>(message => message.Contains("store-20260326.bak", StringComparison.Ordinal)),
            "Restore Backup",
            "Restore Backup",
            "Cancel");
        await _backupService.DidNotReceive().RestoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _auditService.DidNotReceive().LogAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestRestoreCommand_WhenConfirmed_ShouldRestoreAndAudit()
    {
        var selectedBackup = new BackupFileInfo("store-20260326.bak", @"C:\Backups\store-20260326.bak", DateTime.Today, 2048);

        _dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        _backupService.RestoreAsync(selectedBackup.FullPath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BackupResult(true, "Database restored successfully.", selectedBackup.FullPath)));

        var sut = CreateSut();
        sut.SelectedBackup = selectedBackup;

        await sut.RequestRestoreCommand.ExecuteAsync(null);

        Assert.Equal("Database restored successfully.", sut.SuccessMessage);
        await _backupService.Received(1).RestoreAsync(selectedBackup.FullPath, Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync(
            "Restore",
            "Database",
            null,
            null,
            selectedBackup.FileName,
            UserType.Admin.ToString(),
            "Database restored from backup",
            Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<SalesDataChangedEvent>());
    }
}
