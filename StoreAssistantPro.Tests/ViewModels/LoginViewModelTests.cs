using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Users.Commands;
using StoreAssistantPro.Modules.Users.Services;

namespace StoreAssistantPro.Tests.ViewModels;

public class LoginViewModelTests
{
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IConnectivityMonitorService _connectivity = Substitute.For<IConnectivityMonitorService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();

    private LoginViewModel CreateSut()
    {
        _regional.Now.Returns(DateTime.Now);
        _regional.FormatTime(Arg.Any<DateTime>()).Returns("12:00 PM");
        _connectivity.IsConnected.Returns(true);
        _userService.HasUserRoleAsync(Arg.Any<CancellationToken>()).Returns(true);
        var vm = new LoginViewModel(_commandBus, _appState, _regional, _connectivity, _userService);
        vm.Initialize();
        return vm;
    }

    // ── User selection ───────────────────────────────────────────────

    [Fact]
    public void SelectUser_SetsSelectedUserType()
    {
        var sut = CreateSut();

        sut.SelectUserCommand.Execute(UserType.Admin);

        Assert.Equal(UserType.Admin, sut.SelectedUserType);
        Assert.True(sut.IsUserSelected);
    }

    [Fact]
    public async Task Initialize_When_NoUserRoleConfigured_HidesUserRole_And_SelectsAdmin()
    {
        _userService.HasUserRoleAsync(Arg.Any<CancellationToken>()).Returns(false);
        var sut = new LoginViewModel(_commandBus, _appState, _regional, _connectivity, _userService);

        sut.Initialize();
        await Task.Delay(50);

        Assert.False(sut.IsUserRoleVisible);
        Assert.Equal(UserType.Admin, sut.SelectedUserType);
    }

    [Fact]
    public void SelectUser_ClearsPinAndError()
    {
        var sut = CreateSut();
        sut.PinPad.AddDigitCommand.Execute("1");
        sut.SelectUserCommand.Execute(UserType.Admin);

        Assert.Empty(sut.PinPad.Pin);
        Assert.Empty(sut.ErrorMessage);
    }

    // ── PIN pad commands ─────────────────────────────────────────────

    [Fact]
    public void PinDigit_AppendsDigit()
    {
        var sut = CreateSut();

        sut.PinPad.AddDigitCommand.Execute("5");

        Assert.Equal("5", sut.PinPad.Pin);
        Assert.Equal(1, sut.PinPad.PinLength);
    }

    [Fact]
    public void PinDigit_StopsAtFourDigits_WhenNoUserSelected()
    {
        var sut = CreateSut();

        sut.PinPad.AddDigitCommand.Execute("1");
        sut.PinPad.AddDigitCommand.Execute("2");
        sut.PinPad.AddDigitCommand.Execute("3");
        sut.PinPad.AddDigitCommand.Execute("4");
        sut.PinPad.AddDigitCommand.Execute("5");

        Assert.Equal(4, sut.PinPad.PinLength);
    }

    [Fact]
    public void PinBackspace_RemovesLastDigit()
    {
        var sut = CreateSut();
        sut.PinPad.AddDigitCommand.Execute("1");
        sut.PinPad.AddDigitCommand.Execute("2");

        sut.PinPad.BackspaceCommand.Execute(null);

        Assert.Equal("1", sut.PinPad.Pin);
    }

    [Fact]
    public void PinBackspace_DoesNothingWhenEmpty()
    {
        var sut = CreateSut();

        sut.PinPad.BackspaceCommand.Execute(null);

        Assert.Empty(sut.PinPad.Pin);
    }

    [Fact]
    public void PinClear_ResetsPin()
    {
        var sut = CreateSut();
        sut.PinPad.AddDigitCommand.Execute("1");
        sut.PinPad.AddDigitCommand.Execute("2");

        sut.PinPad.ClearCommand.Execute(null);

        Assert.Empty(sut.PinPad.Pin);
    }

    // ── Auto-login on 4th digit ──────────────────────────────────────

    [Fact]
    public async Task FourthDigit_WithUserSelected_AutoTriggersLogin()
    {
        bool? closeResult = null;
        _commandBus.SendAsync(Arg.Any<LoginUserCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.LoginSucceeded = _ => { closeResult = true; return Task.CompletedTask; };
        sut.SelectUserCommand.Execute(UserType.Admin);

        sut.PinPad.AddDigitCommand.Execute("1");
        sut.PinPad.AddDigitCommand.Execute("2");
        sut.PinPad.AddDigitCommand.Execute("3");
        sut.PinPad.AddDigitCommand.Execute("4");

        await Task.Delay(50);

        Assert.True(closeResult);
        await _commandBus.Received(1).SendAsync(
            Arg.Is<LoginUserCommand>(c => c.UserType == UserType.Admin && c.Pin == "1234"));
    }

    // ── Login success ────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidPin_ClosesWithTrue()
    {
        bool? closeResult = null;
        _commandBus.SendAsync(Arg.Any<LoginUserCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.LoginSucceeded = _ => { closeResult = true; return Task.CompletedTask; };
        sut.SelectUserCommand.Execute(UserType.Admin);
        sut.PinPad.AddDigitCommand.Execute("1");
        sut.PinPad.AddDigitCommand.Execute("2");
        sut.PinPad.AddDigitCommand.Execute("3");
        sut.PinPad.AddDigitCommand.Execute("4");
        await Task.Delay(50);

        Assert.True(closeResult);
        Assert.Equal(UserType.Admin, sut.SelectedUserType);
    }

    // ── Login failure ────────────────────────────────────────────────

    [Fact]
    public async Task Login_InvalidPin_ShowsErrorAndClearsPin()
    {
        _commandBus.SendAsync(Arg.Any<LoginUserCommand>())
            .Returns(CommandResult.Failure("Invalid PIN. Try again."));

        var sut = CreateSut();
        sut.SelectUserCommand.Execute(UserType.Admin);
        sut.PinPad.AddDigitCommand.Execute("0");
        sut.PinPad.AddDigitCommand.Execute("0");
        sut.PinPad.AddDigitCommand.Execute("0");
        sut.PinPad.AddDigitCommand.Execute("0");

        await Task.Delay(50);

        Assert.Equal("Invalid PIN. Try again.", sut.ErrorMessage);
        Assert.Empty(sut.PinPad.Pin);
        Assert.Equal(UserType.Admin, sut.SelectedUserType);
    }

    // ── Login without user selected ──────────────────────────────────

    [Fact]
    public async Task Login_NoUserSelected_ShowsError()
    {
        var sut = CreateSut();
        sut.PinPad.AddDigitCommand.Execute("1");
        sut.PinPad.AddDigitCommand.Execute("2");
        sut.PinPad.AddDigitCommand.Execute("3");

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Please select a user.", sut.ErrorMessage);
        await _commandBus.DidNotReceive().SendAsync(Arg.Any<LoginUserCommand>());
    }

    // ── Login without PIN ────────────────────────────────────────────

    [Fact]
    public async Task Login_EmptyPin_ShowsError()
    {
        var sut = CreateSut();
        sut.SelectUserCommand.Execute(UserType.Admin);

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Please enter your PIN.", sut.ErrorMessage);
        await _commandBus.DidNotReceive().SendAsync(Arg.Any<LoginUserCommand>());
    }

    [Fact]
    public async Task Initialize_WhenCalledTwice_DoesNotDuplicateAutoLogin()
    {
        _commandBus.SendAsync(Arg.Any<LoginUserCommand>())
            .Returns(CommandResult.Success());

        _userService.HasUserRoleAsync(Arg.Any<CancellationToken>()).Returns(true);
        var sut = new LoginViewModel(_commandBus, _appState, _regional, _connectivity, _userService);
        sut.Initialize();
        sut.Initialize();
        sut.SelectUserCommand.Execute(UserType.Admin);

        sut.PinPad.AddDigitCommand.Execute("1");
        sut.PinPad.AddDigitCommand.Execute("2");
        sut.PinPad.AddDigitCommand.Execute("3");
        sut.PinPad.AddDigitCommand.Execute("4");

        await Task.Delay(50);

        await _commandBus.Received(1).SendAsync(
            Arg.Is<LoginUserCommand>(c => c.UserType == UserType.Admin && c.Pin == "1234"));
    }

    [Fact]
    public void Dispose_ClearsLoginSucceeded()
    {
        var sut = CreateSut();
        sut.LoginSucceeded = _ => Task.CompletedTask;

        sut.Dispose();

        Assert.Null(sut.LoginSucceeded);
    }

    [Fact]
    public async Task Login_InFlight_ExposesSharedWorkingState()
    {
        var pendingLogin = new TaskCompletionSource<CommandResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _commandBus.SendAsync(Arg.Any<LoginUserCommand>())
            .Returns(_ => pendingLogin.Task);

        var sut = CreateSut();
        sut.SelectUserCommand.Execute(UserType.Admin);
        sut.PinPad.AddDigitCommand.Execute("1");
        sut.PinPad.AddDigitCommand.Execute("2");
        sut.PinPad.AddDigitCommand.Execute("3");
        sut.PinPad.AddDigitCommand.Execute("4");

        await Task.Yield();

        Assert.True(sut.IsBusy);
        Assert.True(sut.IsWorking);
        Assert.Equal("Verifying login...", sut.WorkingMessage);

        pendingLogin.SetResult(CommandResult.Failure("Invalid PIN."));
        await Task.Delay(50);

        Assert.False(sut.IsWorking);
    }

    [Fact]
    public async Task ResetPin_InFlight_UsesResetWorkingMessage()
    {
        var pendingReset = new TaskCompletionSource<CommandResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _commandBus.SendAsync(Arg.Any<ChangePinCommand>())
            .Returns(_ => pendingReset.Task);

        var sut = CreateSut();
        sut.SelectUserCommand.Execute(UserType.Admin);
        sut.ForgotPinCommand.Execute(null);
        sut.MasterPassword = "123456";
        sut.NewPin = "5831";
        sut.NewPinConfirm = "5831";

        var resetTask = sut.ResetPinCommand.ExecuteAsync(null);
        await Task.Yield();

        Assert.True(sut.IsBusy);
        Assert.True(sut.IsWorking);
        Assert.Equal("Resetting PIN...", sut.WorkingMessage);

        pendingReset.SetResult(CommandResult.Success());
        await resetTask;

        Assert.False(sut.IsWorking);
    }
}
