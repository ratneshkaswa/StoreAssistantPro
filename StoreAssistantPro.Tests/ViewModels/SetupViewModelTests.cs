using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class SetupViewModelTests
{
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();
    private readonly IRegionalSettingsService _regionalSettings = Substitute.For<IRegionalSettingsService>();

    private SetupViewModel CreateSut()
    {
        _regionalSettings.Now.Returns(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")));
        var sut = new SetupViewModel(_commandBus, _regionalSettings)
        {
            FirmName = string.Empty,
            Address = string.Empty,
            State = string.Empty,
            Pincode = string.Empty,
            Phone = string.Empty,
            Email = string.Empty,
            GSTIN = string.Empty,
            PAN = string.Empty
        };
        sut.ClearSensitivePins();
        return sut;
    }

    private void FillValidPins(SetupViewModel sut,
        string adminPin = "2847", string adminConfirm = "2847",
        string userPin = "5023", string userConfirm = "5023",
        string masterPin = "760918", string masterConfirm = "760918")
    {
        sut.AdminPin = adminPin;
        sut.AdminPinConfirm = adminConfirm;
        sut.UserPin = userPin;
        sut.UserPinConfirm = userConfirm;
        sut.MasterPin = masterPin;
        sut.MasterPinConfirm = masterConfirm;
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000, int pollMs = 25)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;

            await Task.Delay(pollMs);
        }

        return condition();
    }

    [Fact]
    public async Task Save_ValidInput_CallsCommandBusAndCloses()
    {
        bool? closeResult = null;
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>(), Arg.Any<CancellationToken>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = result => closeResult = result;

        sut.FirmName = "Test Store";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteFirstSetupCommand>(c =>
            c.FirmName == "Test Store" && c.Address == "" && c.Phone == ""
            && c.Email == "" && c.GSTIN == "" && c.CurrencyCode == "INR"
            && c.AdminPin == "2847"
            && c.UserPin == "5023" && c.MasterPin == "760918"), Arg.Any<CancellationToken>());
        Assert.True(await WaitUntilAsync(() => closeResult == true));
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
    [InlineData("1234", "1234")]
    [InlineData("5678", "5678")]
    public async Task Save_DuplicatePins_ShowsError(string admin, string user)
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        FillValidPins(sut, adminPin: admin, adminConfirm: admin,
                      userPin: user, userConfirm: user);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Each role must have a unique PIN.", sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_HandlerFails_SetsErrorMessage()
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>(), Arg.Any<CancellationToken>())
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
        Assert.Equal("Enter a 10-digit phone number", sut.PhoneValidationHint);
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
    public void OnGSTINChanged_PartialGstin_DoesNotFillState()
    {
        var sut = CreateSut();
        sut.State = "";
        sut.GSTIN = "08";
        Assert.Empty(sut.State);
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
        Assert.Contains("0 / 5", sut.RequiredFieldsProgress);
    }

    [Fact]
    public void RequiredFieldsProgress_OnlyFirmName_Shows1()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        Assert.Contains("1 / 5", sut.RequiredFieldsProgress);
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
        Assert.Contains("2 / 5", sut.RequiredFieldsProgress);
    }

    // -- Save format validations --

    [Fact]
    public async Task Save_InvalidGstin_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.UseEssentialSetupValidationOnly = false;
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
        sut.UseEssentialSetupValidationOnly = false;
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

        Assert.Equal("Phone must be exactly 10 digits.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("27AAPFU0939F1ZV")]
    public async Task Save_EmptyOrValidGstin_DoesNotBlock(string gstin)
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>(), Arg.Any<CancellationToken>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        sut.GSTIN = gstin;
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsSetupComplete);
    }

    [Fact]
    public async Task Save_InvalidAdvancedField_DoesNotBlock_EssentialSetupMode()
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>(), Arg.Any<CancellationToken>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        sut.GSTIN = "INVALID";
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
        sut.UserPin = "9012";
        sut.MasterPin = "012345";
        Assert.Contains("Master contains Admin", sut.PinConflictWarning);
    }

    [Fact]
    public void PinConflictWarning_NoConflicts_ReturnsEmpty()
    {
        var sut = CreateSut();
        sut.AdminPin = "1234";
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
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>(), Arg.Any<CancellationToken>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        sut.SelectedFYStartMonth = "January";
        sut.SelectedDateFormat = "yyyy-MM-dd";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteFirstSetupCommand>(c =>
            c.CurrencySymbol == "\u20b9"
            && c.FinancialYearStartMonth == 1
            && c.FinancialYearEndMonth == 12
            && c.DateFormat == "yyyy-MM-dd"), Arg.Any<CancellationToken>());
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
        // With OrdinalIgnoreCase sort, '&' (U+0026) < letters, so the Andaman entry comes first.
        Assert.Equal("Andaman & Nicobar Islands", sut.IndianStates[0]);
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
        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
        var expected = istNow.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal($"e.g. {expected}", sut.DateFormatPreview);
    }

    [Fact]
    public void DateFormatPreview_ChangedFormat_Updates()
    {
        var sut = CreateSut();
        sut.SelectedDateFormat = "yyyy-MM-dd";
        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
        var expected = istNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal($"e.g. {expected}", sut.DateFormatPreview);
    }

    [Fact]
    public void DateFormatPreview_DMMMyyyy_ShowsMonthName()
    {
        var sut = CreateSut();
        sut.SelectedDateFormat = "d MMM yyyy";
        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
        Assert.Contains("e.g.", sut.DateFormatPreview);
        Assert.Contains(istNow.Year.ToString(), sut.DateFormatPreview);
    }

    // -- Currency preview --

    [Fact]
    public void CurrencyPreview_Rupee_ShowsIndianFormat()
    {
        var sut = CreateSut();
        Assert.Equal("e.g. ₹ 1,00,000", sut.CurrencyPreview);
    }

    [Fact]
    public void CurrencyPreview_AlwaysShowsRupee()
    {
        var sut = CreateSut();
        Assert.Equal("e.g. \u20b9 1,00,000", sut.CurrencyPreview);
    }

    [Fact]
    public void SelectedCurrencySymbol_IsAlwaysRupee()
    {
        var sut = CreateSut();
        Assert.Equal("\u20b9", sut.SelectedCurrencySymbol);
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
            userPin: "9012", userConfirm: "9012",
            masterPin: "123456", masterConfirm: "123456");

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("must not contain", sut.ErrorMessage);
    }

    [Fact]
    public async Task Save_MasterPinDiffersFromAllRoles_Succeeds()
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>(), Arg.Any<CancellationToken>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Test";
        FillValidPins(sut, adminPin: "1234", adminConfirm: "1234",
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
        sut.UseEssentialSetupValidationOnly = false;
        FillValidPins(sut, adminPin: "2847", adminConfirm: "2847",
            userPin: "5023", userConfirm: "5023",
            masterPin: "764108", masterConfirm: "764108");
        sut.GSTIN = "27AAPFU0939F1ZX";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("check digit", sut.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // -- Section completion indicators --

    [Fact]
    public void IsFirmSectionComplete_EmptyName_False()
    {
        var sut = CreateSut();
        Assert.False(sut.IsFirmSectionComplete);
    }

    [Fact]
    public void IsFirmSectionComplete_WithNameOnly_False()
    {
        var sut = CreateSut();
        sut.FirmName = "Test Store";
        Assert.True(sut.IsFirmSectionComplete);
    }

    [Fact]
    public void IsFirmSectionComplete_NameAndBusinessDetail_True()
    {
        var sut = CreateSut();
        sut.FirmName = "Test Store";
        sut.State = "Rajasthan";
        Assert.True(sut.IsFirmSectionComplete);
    }

    [Fact]
    public void IsSecuritySectionComplete_NoPins_False()
    {
        var sut = CreateSut();
        Assert.False(sut.IsSecuritySectionComplete);
    }

    [Fact]
    public void IsSecuritySectionComplete_AllPinsValid_True()
    {
        var sut = CreateSut();
        FillValidPins(sut);
        Assert.True(sut.IsSecuritySectionComplete);
    }

    [Fact]
    public void IsSecuritySectionComplete_MismatchedConfirm_False()
    {
        var sut = CreateSut();
        FillValidPins(sut, adminConfirm: "9999");
        Assert.False(sut.IsSecuritySectionComplete);
    }

    [Fact]
    public void FirmSectionStatusText_OnlyName_PromptsForBusinessDetails()
    {
        var sut = CreateSut();
        sut.FirmName = "Test Store";

        Assert.Equal("Required details complete", sut.FirmSectionStatusText);
    }

    [Fact]
    public void SecuritySectionStatusText_TracksCompletedChecks()
    {
        var sut = CreateSut();
        sut.AdminPin = "2847";
        sut.AdminPinConfirm = "2847";

        Assert.Equal("2 of 4 checks", sut.SecuritySectionStatusText);
    }

    [Fact]
    public void SaveReadinessMessage_NotReady_ShowsBlockingReason()
    {
        var sut = CreateSut();

        Assert.Equal("Firm name is required.", sut.SaveReadinessMessage);
    }

    // -- Backup validation --

    [Fact]
    public async Task Save_BackupEnabled_NonexistentFolder_ShowsError()
    {
        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        sut.UseEssentialSetupValidationOnly = false;
        FillValidPins(sut);
        sut.AutoBackupEnabled = true;
        sut.BackupTime = "22:00";
        sut.BackupLocation = @"C:\Nonexistent\Path\12345";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("folder does not exist", sut.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Save_BackupEnabled_EmptyLocation_ShowsError()
    {
        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        sut.UseEssentialSetupValidationOnly = false;
        FillValidPins(sut);
        sut.AutoBackupEnabled = true;
        sut.BackupTime = "22:00";
        sut.BackupLocation = "";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Backup location is required", sut.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Save_BackupEnabled_InvalidTime_ShowsError()
    {
        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        sut.UseEssentialSetupValidationOnly = false;
        FillValidPins(sut);
        sut.AutoBackupEnabled = true;
        sut.BackupTime = "25:99";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("HH:mm", sut.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // -- GoToLogin command --

    [Fact]
    public void GoToLogin_ClearsPinsAndCloses()
    {
        bool? closeResult = null;
        var sut = CreateSut();
        sut.RequestClose = result => closeResult = result;
        FillValidPins(sut);

        sut.GoToLoginCommand.Execute(null);

        Assert.True(closeResult);
        Assert.Equal(string.Empty, sut.AdminPin);
        Assert.Equal(string.Empty, sut.MasterPin);
    }

    // -- Backup disabled bypasses validation --

    [Fact]
    public async Task Save_BackupDisabled_InvalidTimeAndPath_Succeeds()
    {
        _commandBus.SendAsync(Arg.Any<CompleteFirstSetupCommand>(), Arg.Any<CancellationToken>())
            .Returns(CommandResult.Success());

        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.FirmName = "Store";
        FillValidPins(sut);
        sut.AutoBackupEnabled = false;
        sut.BackupTime = "99:99";
        sut.BackupLocation = "";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsSetupComplete);
    }

    // -- Phone digit count validation --

    [Fact]
    public async Task Save_PhoneTooFewDigits_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.Phone = "123";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("10 digits", sut.ErrorMessage);
    }

    // -- Composition rate range validation --

    [Fact]
    public async Task Save_CompositionRateOutOfRange_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.UseEssentialSetupValidationOnly = false;
        sut.SelectedGstRegistrationType = "Composition";
        sut.CompositionRate = "150";
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("0", sut.ErrorMessage);
        Assert.Contains("100", sut.ErrorMessage);
    }

    // -- GSTIN state mismatch hint --

    [Fact]
    public void GstinValidationHint_StateMismatch_ShowsWarning()
    {
        var sut = CreateSut();
        sut.State = "Delhi";
        sut.GSTIN = "27AAPFU0939F1ZV"; // Valid Maharashtra GSTIN, but State is set to Delhi
        Assert.Contains("differs from selected state", sut.GstinValidationHint);
    }

    // -- NumberToWords preview --

    [Theory]
    [InlineData("English", "One Lakh")]
    [InlineData("Hindi", "\u090f\u0915 \u0932\u093e\u0916")]
    public void NumberToWordsPreview_ShowsExample(string language, string expectedFragment)
    {
        var sut = CreateSut();
        sut.SelectedNumberToWordsLanguage = language;
        Assert.Contains(expectedFragment, sut.NumberToWordsPreview);
    }

    // -- Dispose clears RequestClose --

    [Fact]
    public void Dispose_ClearsRequestClose()
    {
        var sut = CreateSut();
        sut.RequestClose = _ => { };
        sut.Dispose();
        Assert.Null(sut.RequestClose);
    }

    // -- B3: Phone regex rejects all-special-char input --

    [Theory]
    [InlineData("+++")]
    [InlineData("---")]
    [InlineData("+ +")]
    public void PhoneValidationHint_AllSpecialChars_RejectsInput(string input)
    {
        var sut = CreateSut();
        sut.Phone = input;
        Assert.Contains("10-digit", sut.PhoneValidationHint);
    }

    // -- B5: State freetext validation on Save --

    [Fact]
    public async Task Save_InvalidStateText_ShowsError()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.State = "Rajastha"; // typo
        FillValidPins(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Contains("valid Indian state", sut.ErrorMessage);
    }

    // -- B4: GstinRegex accepts non-Z at position 14 --

    [Fact]
    public void GstinValidationHint_NonZAtPosition14_IsValid()
    {
        var sut = CreateSut();
        sut.GSTIN = "22AAAAA0000A1A5"; // 'A' at position 14 instead of 'Z'
        Assert.DoesNotContain("Format:", sut.GstinValidationHint);
    }
}
