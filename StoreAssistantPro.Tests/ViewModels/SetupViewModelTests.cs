using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class SetupViewModelTests
{
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();

    private SetupViewModel CreateSut() => new(_commandBus);

    private void FillValidPins(SetupViewModel sut,
        string adminPin = "1234", string adminConfirm = "1234",
        string managerPin = "5678", string managerConfirm = "5678",
        string userPin = "9012", string userConfirm = "9012",
        string masterPin = "123456", string masterConfirm = "123456")
    {
        sut.AdminPin = adminPin;
        sut.AdminPinConfirm = adminConfirm;
        sut.ManagerPin = managerPin;
        sut.ManagerPinConfirm = managerConfirm;
        sut.UserPin = userPin;
        sut.UserPinConfirm = userConfirm;
        sut.MasterPin = masterPin;
        sut.MasterPinConfirm = masterConfirm;
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
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteFirstSetupCommand>(c =>
            c.FirmName == "Test Store" && c.Address == "" && c.Phone == ""
            && c.Email == "" && c.GSTIN == "" && c.CurrencyCode == "INR"
            && c.AdminPin == "1234" && c.ManagerPin == "5678"
            && c.UserPin == "9012" && c.MasterPin == "123456"));
        Assert.True(closeResult);
    }

    [Fact]
    public async Task Save_EmptyFirmName_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Firm name is required.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("12", "Admin PIN must be exactly 4 digits.")]
    [InlineData("abcd", "Admin PIN must be exactly 4 digits.")]
    [InlineData("12345", "Admin PIN must be exactly 4 digits.")]
    public async Task Save_InvalidAdminPin_ShowsError(string pin, string expectedError)
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        FillValidPins(sut, adminPin: pin, adminConfirm: pin);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal(expectedError, sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_PinConfirmMismatch_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        FillValidPins(sut, adminConfirm: "9999");

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Admin PIN confirmation does not match.", sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_InvalidMasterPin_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        FillValidPins(sut, masterPin: "1234", masterConfirm: "1234");

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Master Password must be exactly 6 digits.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("1234", "1234", "5678")]
    [InlineData("1234", "5678", "1234")]
    [InlineData("5678", "1234", "1234")]
    public async Task Save_DuplicatePins_ShowsError(string admin, string manager, string user)
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        FillValidPins(sut, adminPin: admin, adminConfirm: admin,
                      managerPin: manager, managerConfirm: manager,
                      userPin: user, userConfirm: user);

        await sut.SaveCommand.ExecuteAsync(null);

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
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Already initialized.", sut.ErrorMessage);
        Assert.Null(closeResult);
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

    // -- GSTIN state decode --

    [Theory]
    [InlineData("22AAAAA0000A1Z5", "Chhattisgarh")]
    [InlineData("27AAAAA0000A1Z5", "Maharashtra")]
    [InlineData("07AAAAA0000A1Z5", "Delhi")]
    public void GstinValidationHint_ValidGstin_ShowsStateName(string gstin, string expectedState)
    {
        var sut = CreateSut();
        sut.GSTIN = gstin;
        Assert.Contains(expectedState, sut.GstinValidationHint);
    }

    [Fact]
    public void GstinValidationHint_InvalidGstin_ShowsFormatHelp()
    {
        var sut = CreateSut();
        sut.GSTIN = "INVALID";
        Assert.Equal("Format: 22AAAAA0000A1Z5", sut.GstinValidationHint);
    }

    [Fact]
    public void GstinValidationHint_Empty_ReturnsEmpty()
    {
        var sut = CreateSut();
        sut.GSTIN = "";
        Assert.Empty(sut.GstinValidationHint);
    }

    // -- Phone format preview --

    [Fact]
    public void PhoneValidationHint_10Digits_ShowsFormattedPreview()
    {
        var sut = CreateSut();
        sut.Phone = "9876543210";
        Assert.Contains("98765 43210", sut.PhoneValidationHint);
    }

    [Fact]
    public void PhoneValidationHint_InvalidChars_ShowsWarning()
    {
        var sut = CreateSut();
        sut.Phone = "abc";
        Assert.Equal("Digits, +, - and spaces only", sut.PhoneValidationHint);
    }

    // -- Display properties (Not provided fallback) --

    [Theory]
    [InlineData("", "Not provided")]
    [InlineData("  ", "Not provided")]
    [InlineData("123 Main St", "123 Main St")]
    public void AddressDisplay_FallbackWhenBlank(string address, string expected)
    {
        var sut = CreateSut();
        sut.Address = address;
        Assert.Equal(expected, sut.AddressDisplay);
    }

    [Theory]
    [InlineData("", "Not provided")]
    [InlineData("test@test.com", "test@test.com")]
    public void EmailDisplay_FallbackWhenBlank(string email, string expected)
    {
        var sut = CreateSut();
        sut.Email = email;
        Assert.Equal(expected, sut.EmailDisplay);
    }

    // -- Currency code (hardcoded INR for India) --

    [Fact]
    public void CurrencyCode_AlwaysINR()
    {
        var sut = CreateSut();
        Assert.Equal("INR", sut.CurrencyCode);
    }

    // -- Redirect countdown --

    [Fact]
    public void RedirectCountdown_InitiallyEmpty()
    {
        var sut = CreateSut();
        Assert.Empty(sut.RedirectCountdown);
    }
}