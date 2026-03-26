using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Users.Commands;
using StoreAssistantPro.Modules.Users.Services;
using StoreAssistantPro.Modules.Users.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class UsersViewModelTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();

    private UsersViewModel CreateSut() => new(_userService, _commandBus);

    [Fact]
    public async Task LoadUsers_PopulatesList()
    {
        var users = new List<UserCredential>
        {
            new() { Id = 1, UserType = UserType.Admin, PinHash = "hash1" },
            new() { Id = 2, UserType = UserType.User, PinHash = "hash2" }
        };
        _userService.GetAllUsersAsync(Arg.Any<CancellationToken>()).Returns(users);

        var sut = CreateSut();
        await sut.LoadUsersCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Users.Count);
    }

    [Fact]
    public void ChangePinCommand_RequiresSelectedUser()
    {
        var sut = CreateSut();

        Assert.False(sut.ChangePinCommand.CanExecute(null));
    }

    [Fact]
    public void ChangePinCommand_RequiresValidUserPinShape()
    {
        var sut = CreateSut();
        sut.SelectedUser = new UserCredential { UserType = UserType.User };
        sut.NewPin = "12";
        sut.ConfirmPin = "12";

        Assert.False(sut.ChangePinCommand.CanExecute(null));
    }

    [Fact]
    public void ChangePinCommand_AdminRequiresValidMasterPinShape()
    {
        var sut = CreateSut();
        sut.SelectedUser = new UserCredential { UserType = UserType.Admin };
        sut.NewPin = "1234";
        sut.ConfirmPin = "1234";
        sut.MasterPin = "123";

        Assert.False(sut.ChangePinCommand.CanExecute(null));
    }

    [Fact]
    public async Task ChangePin_Mismatch_SetsError()
    {
        var sut = CreateSut();
        sut.SelectedUser = new UserCredential { UserType = UserType.User };
        sut.NewPin = "2580";
        sut.ConfirmPin = "7392";

        await sut.ChangePinCommand.ExecuteAsync(null);

        Assert.Equal("PINs do not match.", sut.ErrorMessage);
    }

    [Fact]
    public async Task ChangePin_User_Succeeds()
    {
        _commandBus.SendAsync(Arg.Any<ChangePinCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.SelectedUser = new UserCredential { UserType = UserType.User };
        sut.NewPin = "2580";
        sut.ConfirmPin = "2580";

        await sut.ChangePinCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<ChangePinCommand>(c =>
            c.UserType == UserType.User && c.NewPin == "2580" && c.MasterPin == null));
        Assert.Contains("User PIN changed", sut.SuccessMessage);
        Assert.Empty(sut.NewPin);
        Assert.Empty(sut.ConfirmPin);
    }

    [Fact]
    public async Task ChangePin_Admin_InvalidMasterPin_SetsError()
    {
        _commandBus.SendAsync(Arg.Any<ChangePinCommand>())
            .Returns(CommandResult.Failure("Invalid Master PIN."));

        var sut = CreateSut();
        sut.SelectedUser = new UserCredential { UserType = UserType.Admin };
        sut.NewPin = "2580";
        sut.ConfirmPin = "2580";
        sut.MasterPin = "000000";

        await sut.ChangePinCommand.ExecuteAsync(null);

        Assert.Equal("Invalid Master PIN.", sut.ErrorMessage);
        Assert.Empty(sut.MasterPin);
    }

    [Fact]
    public async Task ChangePin_Admin_ValidMasterPin_Succeeds()
    {
        _commandBus.SendAsync(Arg.Any<ChangePinCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.SelectedUser = new UserCredential { UserType = UserType.Admin };
        sut.NewPin = "2580";
        sut.ConfirmPin = "2580";
        sut.MasterPin = "123456";

        await sut.ChangePinCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<ChangePinCommand>(c =>
            c.UserType == UserType.Admin && c.NewPin == "2580" && c.MasterPin == "123456"));
        Assert.Contains("Admin PIN changed", sut.SuccessMessage);
    }

    [Fact]
    public void SelectingUser_ClearsFormAndShowsMasterPinForAdmin()
    {
        var sut = CreateSut();
        sut.NewPin = "leftover";
        sut.ErrorMessage = "old error";

        sut.SelectedUser = new UserCredential { UserType = UserType.Admin };

        Assert.Empty(sut.NewPin);
        Assert.Empty(sut.ErrorMessage);
        Assert.True(sut.IsMasterPinRequired);
    }

    [Fact]
    public void SelectingUser_HidesMasterPinForNonAdmin()
    {
        var sut = CreateSut();

        sut.SelectedUser = new UserCredential { UserType = UserType.User };

        Assert.False(sut.IsMasterPinRequired);
    }
}
