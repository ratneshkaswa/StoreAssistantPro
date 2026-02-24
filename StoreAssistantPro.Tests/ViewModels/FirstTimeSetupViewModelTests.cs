using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class FirstTimeSetupViewModelTests
{
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();

    private FirstTimeSetupViewModel CreateSut() => new(_commandBus);

    /// <summary>
    /// Helper: configures a valid SUT through steps 1→2→3 and executes SaveCommand.
    /// </summary>
    private async Task<FirstTimeSetupViewModel> CreateAndCompleteSetupAsync(
        string firmName = "Test Store",
        string adminPin = "1234", string adminConfirm = "1234",
        string managerPin = "5678", string managerConfirm = "5678",
        string userPin = "9012", string userConfirm = "9012",
        string masterPin = "123456", string masterConfirm = "123456",
        bool? expectedClose = true)
    {
        bool? closeResult = null;
        var sut = CreateSut();
        sut.RequestClose = result => closeResult = result;

        // Step 1: Firm details
        sut.FirmName = firmName;
        sut.NextStepCommand.Execute(null);

        // Step 2: PINs
        sut.AdminPin = adminPin;
        sut.AdminPinConfirm = adminConfirm;
        sut.ManagerPin = managerPin;
        sut.ManagerPinConfirm = managerConfirm;
        sut.UserPin = userPin;
        sut.UserPinConfirm = userConfirm;
        sut.MasterPin = masterPin;
        sut.MasterPinConfirm = masterConfirm;
        sut.NextStepCommand.Execute(null);

        // Step 3: Save
        if (sut.IsStep3)
            await sut.SaveCommand.ExecuteAsync(null);

        return sut;
    }

    [Fact]
    public async Task Save_ValidInput_CallsCommandBusAndCloses()
    {
        bool? closeResult = null;
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = result => closeResult = result;

        sut.FirmName = "Test Store";
        sut.NextStepCommand.Execute(null);
        Assert.True(sut.IsStep2);

        sut.AdminPin = "1234";
        sut.AdminPinConfirm = "1234";
        sut.ManagerPin = "5678";
        sut.ManagerPinConfirm = "5678";
        sut.UserPin = "9012";
        sut.UserPinConfirm = "9012";
        sut.MasterPin = "123456";
        sut.MasterPinConfirm = "123456";
        sut.NextStepCommand.Execute(null);
        Assert.True(sut.IsStep3);

        await sut.SaveCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteFirstSetupCommand>(c =>
            c.FirmName == "Test Store" && c.Address == "" && c.Phone == ""
            && c.AdminPin == "1234" && c.ManagerPin == "5678"
            && c.UserPin == "9012" && c.MasterPin == "123456"));
        Assert.True(closeResult);
    }

    [Fact]
    public void NextStep_EmptyFirmName_StaysOnStep1()
    {
        var sut = CreateSut();
        sut.FirmName = "";

        sut.NextStepCommand.Execute(null);

        Assert.True(sut.IsStep1);
        Assert.Equal("Firm name is required.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("12", "Admin PIN must be exactly 4 digits.")]
    [InlineData("abcd", "Admin PIN must be exactly 4 digits.")]
    [InlineData("12345", "Admin PIN must be exactly 4 digits.")]
    public void NextStep_InvalidAdminPin_StaysOnStep2(string pin, string expectedError)
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.NextStepCommand.Execute(null);

        sut.AdminPin = pin;
        sut.AdminPinConfirm = pin;
        sut.ManagerPin = "5678";
        sut.ManagerPinConfirm = "5678";
        sut.UserPin = "9012";
        sut.UserPinConfirm = "9012";
        sut.MasterPin = "123456";
        sut.MasterPinConfirm = "123456";
        sut.NextStepCommand.Execute(null);

        Assert.True(sut.IsStep2);
        Assert.Equal(expectedError, sut.ErrorMessage);
    }

    [Fact]
    public void NextStep_PinConfirmMismatch_StaysOnStep2()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.NextStepCommand.Execute(null);

        sut.AdminPin = "1234";
        sut.AdminPinConfirm = "9999";
        sut.ManagerPin = "5678";
        sut.ManagerPinConfirm = "5678";
        sut.UserPin = "9012";
        sut.UserPinConfirm = "9012";
        sut.MasterPin = "123456";
        sut.MasterPinConfirm = "123456";
        sut.NextStepCommand.Execute(null);

        Assert.True(sut.IsStep2);
        Assert.Equal("Admin PIN confirmation does not match.", sut.ErrorMessage);
    }

    [Fact]
    public void NextStep_InvalidMasterPin_StaysOnStep2()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.NextStepCommand.Execute(null);

        sut.AdminPin = "1234";
        sut.AdminPinConfirm = "1234";
        sut.ManagerPin = "5678";
        sut.ManagerPinConfirm = "5678";
        sut.UserPin = "9012";
        sut.UserPinConfirm = "9012";
        sut.MasterPin = "1234";
        sut.MasterPinConfirm = "1234";
        sut.NextStepCommand.Execute(null);

        Assert.True(sut.IsStep2);
        Assert.Equal("Master Password must be exactly 6 digits.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("1234", "1234", "5678")]
    [InlineData("1234", "5678", "1234")]
    [InlineData("5678", "1234", "1234")]
    public void NextStep_DuplicatePins_StaysOnStep2(string admin, string manager, string user)
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.NextStepCommand.Execute(null);

        sut.AdminPin = admin;
        sut.AdminPinConfirm = admin;
        sut.ManagerPin = manager;
        sut.ManagerPinConfirm = manager;
        sut.UserPin = user;
        sut.UserPinConfirm = user;
        sut.MasterPin = "123456";
        sut.MasterPinConfirm = "123456";
        sut.NextStepCommand.Execute(null);

        Assert.True(sut.IsStep2);
        Assert.Equal("Each role must have a unique PIN.", sut.ErrorMessage);
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
        sut.NextStepCommand.Execute(null);

        sut.AdminPin = "1234";
        sut.AdminPinConfirm = "1234";
        sut.ManagerPin = "5678";
        sut.ManagerPinConfirm = "5678";
        sut.UserPin = "9012";
        sut.UserPinConfirm = "9012";
        sut.MasterPin = "123456";
        sut.MasterPinConfirm = "123456";
        sut.NextStepCommand.Execute(null);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Already initialized.", sut.ErrorMessage);
        Assert.Null(closeResult);
    }

    [Fact]
    public void PreviousStep_FromStep2_GoesBackToStep1()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.NextStepCommand.Execute(null);
        Assert.True(sut.IsStep2);

        sut.PreviousStepCommand.Execute(null);
        Assert.True(sut.IsStep1);
    }

    [Theory]
    [InlineData("0000")]
    [InlineData("1234")]
    [InlineData("1111")]
    public void WeakPinWarning_DetectsWeakPins(string pin)
    {
        var sut = CreateSut();
        sut.AdminPin = pin;
        Assert.NotEmpty(sut.AdminPinWarning);
    }

    [Fact]
    public void WeakPinWarning_StrongPinReturnsEmpty()
    {
        var sut = CreateSut();
        sut.AdminPin = "2847";
        Assert.Empty(sut.AdminPinWarning);
    }
}
