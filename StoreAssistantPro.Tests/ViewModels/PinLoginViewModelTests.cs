using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class PinLoginViewModelTests
{
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();

    private PinLoginViewModel CreateSut() => new(_commandBus);

    [Fact]
    public async Task Login_ValidPin_ClosesWithTrue()
    {
        bool? closeResult = null;
        _commandBus.SendAsync(Arg.Any<LoginUserCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = result => closeResult = result;
        sut.UserType = UserType.Admin;
        sut.Pin = "1234";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.True(closeResult);
        Assert.Empty(sut.ErrorMessage);
    }

    [Fact]
    public async Task Login_InvalidPin_ShowsError()
    {
        _commandBus.SendAsync(Arg.Any<LoginUserCommand>())
            .Returns(CommandResult.Failure("Invalid PIN. Try again."));

        var sut = CreateSut();
        sut.UserType = UserType.Manager;
        sut.Pin = "0000";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Invalid PIN. Try again.", sut.ErrorMessage);
        Assert.Empty(sut.Pin);
    }

    [Fact]
    public async Task Login_EmptyPin_ShowsError()
    {
        var sut = CreateSut();
        sut.Pin = "";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Please enter your PIN.", sut.ErrorMessage);
        await _commandBus.DidNotReceive().SendAsync(Arg.Any<LoginUserCommand>());
    }
}
