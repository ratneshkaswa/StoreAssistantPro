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
        string adminPin = "2847", string adminConfirm = "2847",
        string managerPin = "3916", string managerConfirm = "3916",
        string userPin = "5023", string userConfirm = "5023",
        string masterPin = "760918", string masterConfirm = "760918")
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
            && c.AdminPin == "2847" && c.ManagerPin == "3916"
            && c.UserPin == "5023" && c.MasterPin == "760918"));
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

        Assert.Equal("Master PIN must be exactly 6 digits.", sut.ErrorMessage);
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

    // -- Pincode validation hint --

    [Fact]
    public void PincodeValidationHint_Empty_ReturnsEmpty()
    {
        var sut = CreateSut();
        sut.Pincode = "";
        Assert.Empty(sut.PincodeValidationHint);
    }

    [Fact]
    public void PincodeValidationHint_Valid6Digits_ReturnsCheckmark()
    {
        var sut = CreateSut();
        sut.Pincode = "302001";
        Assert.StartsWith("✓", sut.PincodeValidationHint);
    }

    [Theory]
    [InlineData("30200")]
    [InlineData("3020012")]
    [InlineData("abcdef")]
    public void PincodeValidationHint_Invalid_ShowsError(string pincode)
    {
        var sut = CreateSut();
        sut.Pincode = pincode;
        Assert.Contains("6 digits", sut.PincodeValidationHint);
    }

    // -- PAN entity type hint --

    [Theory]
    [InlineData("ABCPD1234E", "Individual")]
    [InlineData("ABCCD1234E", "Company")]
    [InlineData("ABCHD1234E", "HUF")]
    [InlineData("ABCFD1234E", "Firm")]
    [InlineData("ABCTD1234E", "Trust")]
    public void PanValidationHint_ValidPan_ShowsEntityType(string pan, string expectedType)
    {
        var sut = CreateSut();
        sut.PAN = pan;
        Assert.Contains(expectedType, sut.PanValidationHint);
    }

    [Fact]
    public void PanValidationHint_InvalidPan_ShowsFormat()
    {
        var sut = CreateSut();
        sut.PAN = "INVALID";
        Assert.Equal("Format: ABCDE1234F", sut.PanValidationHint);
    }

    // -- Master PIN strength --

    [Theory]
    [InlineData("000000", 1)]
    [InlineData("123456", 1)]
    [InlineData("121212", 2)]
    [InlineData("284731", 3)]
    public void MasterPinStrength_ReturnsCorrectLevel(string pin, int expectedStrength)
    {
        var sut = CreateSut();
        sut.MasterPin = pin;
        Assert.Equal(expectedStrength, sut.MasterPinStrength);
    }

    [Fact]
    public void MasterPinStrength_Empty_ReturnsZero()
    {
        var sut = CreateSut();
        sut.MasterPin = "";
        Assert.Equal(0, sut.MasterPinStrength);
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("123456")]
    [InlineData("999999")]
    public void MasterPinWarning_WeakPins_ShowsWarning(string pin)
    {
        var sut = CreateSut();
        sut.MasterPin = pin;
        Assert.NotEmpty(sut.MasterPinWarning);
    }

    [Fact]
    public void MasterPinWarning_StrongPin_ReturnsEmpty()
    {
        var sut = CreateSut();
        sut.MasterPin = "284731";
        Assert.Empty(sut.MasterPinWarning);
    }

    // -- GSTIN auto-fill State --

    [Fact]
    public void OnGSTINChanged_ValidGstin_AutoFillsEmptyState()
    {
        var sut = CreateSut();
        sut.State = "";
        sut.GSTIN = "08AAAAA0000A1Z5";
        Assert.Equal("Rajasthan", sut.State);
    }

    [Fact]
    public void OnGSTINChanged_ValidGstin_DoesNotOverwriteExistingState()
    {
        var sut = CreateSut();
        sut.State = "Delhi";
        sut.GSTIN = "08AAAAA0000A1Z5";
        Assert.Equal("Delhi", sut.State);
    }

    [Fact]
    public void OnGSTINChanged_InvalidGstin_DoesNotFillState()
    {
        var sut = CreateSut();
        sut.State = "";
        sut.GSTIN = "INVALID";
        Assert.Empty(sut.State);
    }

    // -- GSTIN-PAN cross-validation --

    [Fact]
    public void GstinPanCrossHint_MatchingPan_ShowsCheckmark()
    {
        var sut = CreateSut();
        sut.GSTIN = "22ABCDE1234F1Z5";
        sut.PAN = "ABCDE1234F";
        Assert.StartsWith("✓", sut.GstinPanCrossHint);
    }

    [Fact]
    public void GstinPanCrossHint_MismatchedPan_ShowsWarning()
    {
        var sut = CreateSut();
        sut.GSTIN = "22ABCDE1234F1Z5";
        sut.PAN = "XYZAB9876C";
        Assert.Contains("does not match", sut.GstinPanCrossHint);
    }

    [Theory]
    [InlineData("", "ABCDE1234F")]
    [InlineData("22ABCDE1234F1Z5", "")]
    [InlineData("", "")]
    public void GstinPanCrossHint_MissingField_ReturnsEmpty(string gstin, string pan)
    {
        var sut = CreateSut();
        sut.GSTIN = gstin;
        sut.PAN = pan;
        Assert.Empty(sut.GstinPanCrossHint);
    }

    // -- Required fields progress --

    [Fact]
    public void RequiredFieldsProgress_AllEmpty_ShowsZero()
    {
        var sut = CreateSut();
        Assert.Contains("0 of 5", sut.RequiredFieldsProgress);
    }

    [Fact]
    public void RequiredFieldsProgress_OnlyFirmName_Shows1()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        Assert.Contains("1 of 5", sut.RequiredFieldsProgress);
    }

    [Fact]
    public void RequiredFieldsProgress_AllComplete_ShowsReady()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        FillValidPins(sut);
        Assert.StartsWith("✓", sut.RequiredFieldsProgress);
    }

    [Fact]
    public void RequiredFieldsProgress_PinWithoutConfirm_NotCounted()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.AdminPin = "1234";
        // No confirm
        Assert.Contains("1 of 5", sut.RequiredFieldsProgress);
    }

    // -- Save format validations --

    [Fact]
    public async Task Save_InvalidGstin_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.GSTIN = "INVALID";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("GSTIN format is invalid.", sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_InvalidPan_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.PAN = "INVALID";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("PAN format is invalid.", sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_InvalidPincode_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.Pincode = "123";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Pincode must be exactly 6 digits.", sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_InvalidEmail_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.Email = "not-an-email";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Email format is invalid.", sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_InvalidPhone_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.Phone = "abc";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Phone may only contain digits, +, - and spaces.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("27AAPFU0939F1ZV")]
    public async Task Save_EmptyOrValidGstin_DoesNotBlock(string gstin)
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        sut.GSTIN = gstin;
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsSetupComplete);
    }

    // -- Regional settings defaults --

    [Fact]
    public void RegionalDefaults_CurrencySymbolIsRupee()
    {
        var sut = CreateSut();
        Assert.Equal("₹", sut.SelectedCurrencySymbol);
    }

    [Fact]
    public void RegionalDefaults_FYStartIsApril()
    {
        var sut = CreateSut();
        Assert.Equal("April", sut.SelectedFYStartMonth);
    }

    [Fact]
    public void RegionalDefaults_DateFormatIsDdMmYyyy()
    {
        var sut = CreateSut();
        Assert.Equal("dd/MM/yyyy", sut.SelectedDateFormat);
    }

    [Fact]
    public void FinancialYearDisplay_April_ShowsAprilToMarch()
    {
        var sut = CreateSut();
        sut.SelectedFYStartMonth = "April";
        Assert.Contains("April", sut.FinancialYearDisplay);
        Assert.Contains("March", sut.FinancialYearDisplay);
    }

    [Fact]
    public void FinancialYearDisplay_January_ShowsJanuaryToDecember()
    {
        var sut = CreateSut();
        sut.SelectedFYStartMonth = "January";
        Assert.Contains("January", sut.FinancialYearDisplay);
        Assert.Contains("December", sut.FinancialYearDisplay);
    }

    // -- Pin conflict with Master --

    [Fact]
    public void PinConflictWarning_MasterContainsAdminPin_ShowsWarning()
    {
        var sut = CreateSut();
        sut.AdminPin = "1234";
        sut.ManagerPin = "5678";
        sut.UserPin = "9012";
        sut.MasterPin = "012345";
        Assert.Contains("Master contains Admin", sut.PinConflictWarning);
    }

    [Fact]
    public void PinConflictWarning_NoConflicts_ReturnsEmpty()
    {
        var sut = CreateSut();
        sut.AdminPin = "1234";
        sut.ManagerPin = "5678";
        sut.UserPin = "9012";
        sut.MasterPin = "654789";
        Assert.Empty(sut.PinConflictWarning);
    }

    // -- Email validation hint --

    [Fact]
    public void EmailValidationHint_ValidEmail_ShowsCheckmark()
    {
        var sut = CreateSut();
        sut.Email = "test@example.com";
        Assert.StartsWith("✓", sut.EmailValidationHint);
    }

    [Fact]
    public void EmailValidationHint_InvalidEmail_ShowsError()
    {
        var sut = CreateSut();
        sut.Email = "not-valid";
        Assert.Contains("valid email", sut.EmailValidationHint);
    }

    // -- Save passes regional settings --

    [Fact]
    public async Task Save_PassesRegionalSettings()
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        sut.SelectedCurrencySymbol = "Rs.";
        sut.SelectedFYStartMonth = "January";
        sut.SelectedDateFormat = "yyyy-MM-dd";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteFirstSetupCommand>(c =>
            c.CurrencySymbol == "Rs."
            && c.FinancialYearStartMonth == 1
            && c.FinancialYearEndMonth == 12
            && c.DateFormat == "yyyy-MM-dd"));
    }

    // -- Indian States collection --

    [Fact]
    public void IndianStates_Contains28StatesAnd8UTs()
    {
        var sut = CreateSut();
        Assert.Equal(36, sut.IndianStates.Count);
    }

    [Theory]
    [InlineData("Rajasthan")]
    [InlineData("Delhi")]
    [InlineData("Maharashtra")]
    [InlineData("Ladakh")]
    public void IndianStates_ContainsExpectedEntries(string state)
    {
        var sut = CreateSut();
        Assert.Contains(state, sut.IndianStates);
    }

    [Fact]
    public void IndianStates_IsSortedAlphabetically()
    {
        var sut = CreateSut();
        // States (first 28) should be sorted, then UTs (last 8) sorted
        Assert.Equal("Andhra Pradesh", sut.IndianStates[0]);
    }

    // -- Confirm hint mismatch --

    [Fact]
    public void AdminConfirmHint_Mismatch_ShowsCross()
    {
        var sut = CreateSut();
        sut.AdminPin = "1234";
        sut.AdminPinConfirm = "5678";
        Assert.StartsWith("✗", sut.AdminConfirmHint);
    }

    [Fact]
    public void AdminConfirmHint_Match_ShowsCheckmark()
    {
        var sut = CreateSut();
        sut.AdminPin = "1234";
        sut.AdminPinConfirm = "1234";
        Assert.StartsWith("✓", sut.AdminConfirmHint);
    }

    [Fact]
    public void AdminConfirmHint_PartialConfirm_ReturnsEmpty()
    {
        var sut = CreateSut();
        sut.AdminPin = "1234";
        sut.AdminPinConfirm = "12";
        Assert.Empty(sut.AdminConfirmHint);
    }

    [Fact]
    public void MasterConfirmHint_Mismatch_ShowsCross()
    {
        var sut = CreateSut();
        sut.MasterPin = "123456";
        sut.MasterPinConfirm = "654321";
        Assert.StartsWith("✗", sut.MasterConfirmHint);
    }

    [Fact]
    public void MasterConfirmHint_Match_ShowsCheckmark()
    {
        var sut = CreateSut();
        sut.MasterPin = "123456";
        sut.MasterPinConfirm = "123456";
        Assert.StartsWith("✓", sut.MasterConfirmHint);
    }

    // -- GSTIN auto-fill selects from States list --

    [Fact]
    public void OnGSTINChanged_AutoFilledStateExistsInIndianStates()
    {
        var sut = CreateSut();
        sut.State = "";
        sut.GSTIN = "27AAAAA0000A1Z5";
        Assert.Equal("Maharashtra", sut.State);
        Assert.Contains(sut.State, sut.IndianStates);
    }

    // -- Date format preview --

    [Fact]
    public void DateFormatPreview_Default_ShowsTodayFormatted()
    {
        var sut = CreateSut();
        var expected = DateTime.Today.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal($"e.g. {expected}", sut.DateFormatPreview);
    }

    [Fact]
    public void DateFormatPreview_ChangedFormat_Updates()
    {
        var sut = CreateSut();
        sut.SelectedDateFormat = "yyyy-MM-dd";
        var expected = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal($"e.g. {expected}", sut.DateFormatPreview);
    }

    [Fact]
    public void DateFormatPreview_DMMMyyyy_ShowsMonthName()
    {
        var sut = CreateSut();
        sut.SelectedDateFormat = "d MMM yyyy";
        Assert.Contains("e.g.", sut.DateFormatPreview);
        Assert.Contains(DateTime.Today.Year.ToString(), sut.DateFormatPreview);
    }

    // -- Currency preview --

    [Fact]
    public void CurrencyPreview_Rupee_ShowsIndianFormat()
    {
        var sut = CreateSut();
        Assert.Equal("e.g. ₹ 1,00,000", sut.CurrencyPreview);
    }

    [Fact]
    public void CurrencyPreview_Rs_ShowsRsFormat()
    {
        var sut = CreateSut();
        sut.SelectedCurrencySymbol = "Rs.";
        Assert.Equal("e.g. Rs. 1,00,000", sut.CurrencyPreview);
    }

    // -- #1: GSTIN checksum validation --

    [Fact]
    public void VerifyGstinChecksum_ValidGstin_ReturnsTrue()
    {
        // 27AAPFU0939F1ZV is a known valid GSTIN
        Assert.True(SetupViewModel.VerifyGstinChecksum("27AAPFU0939F1ZV"));
    }

    [Fact]
    public void VerifyGstinChecksum_InvalidCheckDigit_ReturnsFalse()
    {
        Assert.False(SetupViewModel.VerifyGstinChecksum("27AAPFU0939F1ZX"));
    }

    [Fact]
    public void VerifyGstinChecksum_ShortInput_ReturnsFalse()
    {
        Assert.False(SetupViewModel.VerifyGstinChecksum("27AAPFU"));
    }

    [Fact]
    public void GstinValidationHint_InvalidCheckDigit_ShowsWarning()
    {
        var sut = CreateSut();
        sut.GSTIN = "27AAPFU0939F1ZX";
        Assert.Contains("check digit", sut.GstinValidationHint);
        Assert.Contains("Maharashtra", sut.GstinValidationHint);
    }

    [Fact]
    public void GstinValidationHint_ValidCheckDigit_ShowsState()
    {
        var sut = CreateSut();
        sut.GSTIN = "27AAPFU0939F1ZV";
        Assert.StartsWith("\u2713", sut.GstinValidationHint);
    }

    // -- #3: Master PIN = role PIN blocked on Save --

    [Fact]
    public async Task Save_MasterPinContainsAdminPin_Fails()
    {
        var sut = CreateSut();
        sut.FirmName = "Test";
        FillValidPins(sut, adminPin: "1234", adminConfirm: "1234",
            managerPin: "5678", managerConfirm: "5678",
            userPin: "9012", userConfirm: "9012",
            masterPin: "123456", masterConfirm: "123456");

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("must not contain", sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_MasterPinDiffersFromAllRoles_Succeeds()
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Test";
        FillValidPins(sut, adminPin: "1234", adminConfirm: "1234",
            managerPin: "5678", managerConfirm: "5678",
            userPin: "9012", userConfirm: "9012",
            masterPin: "654321", masterConfirm: "654321");

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(string.IsNullOrEmpty(sut.ErrorMessage) || sut.IsSetupComplete);
    }

    // -- #6: Clear ErrorMessage on typing --

    [Fact]
    public void ClearErrorOnEdit_FirmName_ClearsError()
    {
        var sut = CreateSut();
        sut.ErrorMessage = "Some error";
        sut.FirmName = "Changed";
        Assert.Empty(sut.ErrorMessage);
    }

    [Fact]
    public void ClearErrorOnEdit_AdminPin_ClearsError()
    {
        var sut = CreateSut();
        sut.ErrorMessage = "Error from save";
        sut.AdminPin = "1";
        Assert.Empty(sut.ErrorMessage);
    }

    [Fact]
    public void ClearErrorOnEdit_MasterPinConfirm_ClearsError()
    {
        var sut = CreateSut();
        sut.ErrorMessage = "Mismatch";
        sut.MasterPinConfirm = "1";
        Assert.Empty(sut.ErrorMessage);
    }

    [Fact]
    public void ClearErrorOnEdit_NoError_DoesNotThrow()
    {
        var sut = CreateSut();
        sut.FirmName = "Test";
        Assert.Empty(sut.ErrorMessage);
    }

    // -- #1: GSTIN checksum on Save --

    [Fact]
    public async Task Save_InvalidGstinCheckDigit_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Test";
        FillValidPins(sut, adminPin: "2847", adminConfirm: "2847",
            managerPin: "3916", managerConfirm: "3916",
            userPin: "5023", userConfirm: "5023",
            masterPin: "764108", masterConfirm: "764108");
        sut.GSTIN = "27AAPFU0939F1ZX";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("check digit", sut.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}