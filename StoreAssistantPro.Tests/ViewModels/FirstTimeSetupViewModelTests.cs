using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class FirstTimeSetupViewModelTests
{
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();

    private FirstTimeSetupViewModel CreateSut() => new(_commandBus);

    [Fact]
    public async Task Save_ValidInput_CallsCommandBusAndCloses()
    {
        bool? closeResult = null;
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = result => closeResult = result;

        sut.FirmName = "Test Store";
        sut.AdminPin = "1234";
        sut.ManagerPin = "5678";
        sut.UserPin = "9012";
        sut.MasterPin = "123456";

        await sut.SaveCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteFirstSetupCommand>(c =>
            c.FirmName == "Test Store" && c.AdminPin == "1234" && c.ManagerPin == "5678"
            && c.UserPin == "9012" && c.MasterPin == "123456"));
        Assert.True(closeResult);
    }

    [Fact]
    public async Task Save_EmptyFirmName_SetsError()
    {
        var sut = CreateSut();
        sut.FirmName = "";
        sut.AdminPin = "1234";
        sut.ManagerPin = "5678";
        sut.UserPin = "9012";
        sut.MasterPin = "123456";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Firm name is required.", sut.ErrorMessage);
        await _commandBus.DidNotReceive().SendAsync(Arg.Any<CompleteFirstSetupCommand>());
    }

    [Theory]
    [InlineData("12", "Admin PIN must be exactly 4 digits.")]
    [InlineData("abcd", "Admin PIN must be exactly 4 digits.")]
    [InlineData("12345", "Admin PIN must be exactly 4 digits.")]
    public async Task Save_InvalidAdminPin_SetsError(string pin, string expectedError)
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.AdminPin = pin;
        sut.ManagerPin = "5678";
        sut.UserPin = "9012";
        sut.MasterPin = "123456";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal(expectedError, sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_InvalidMasterPin_SetsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.AdminPin = "1234";
        sut.ManagerPin = "5678";
        sut.UserPin = "9012";
        sut.MasterPin = "1234";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Master Password must be exactly 6 digits.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("1234", "1234", "5678")]
    [InlineData("1234", "5678", "1234")]
    [InlineData("5678", "1234", "1234")]
    public async Task Save_DuplicatePins_SetsError(string admin, string manager, string user)
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.AdminPin = admin;
        sut.ManagerPin = manager;
        sut.UserPin = user;
        sut.MasterPin = "123456";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Each role must have a unique PIN.", sut.ErrorMessage);
        await _commandBus.DidNotReceive().SendAsync(Arg.Any<CompleteFirstSetupCommand>());
    }

    [Fact]
    public async Task Save_HandlerFails_SetsErrorMessage()
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>())
            .Returns(CommandResult.Failure("Already initialized."));

        bool? closeResult = null;
        var sut = CreateSut();
        sut.RequestClose = result => closeResult = result;

        sut.FirmName = "Store";
        sut.AdminPin = "1234";
        sut.ManagerPin = "5678";
        sut.UserPin = "9012";
        sut.MasterPin = "123456";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Already initialized.", sut.ErrorMessage);
        Assert.Null(closeResult);
    }
}
