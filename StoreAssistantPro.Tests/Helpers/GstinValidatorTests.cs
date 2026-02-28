using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

/// <summary>
/// Tests for <see cref="GstinValidator"/> — Indian GSTIN format validation.
/// Format: 2-digit state code + 10-char PAN + entity code + 'Z' + check digit.
/// </summary>
public class GstinValidatorTests
{
    // ══════════════════════════════════════════════════════════════════
    //  Valid GSTINs
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("29ABCDE1234F1Z5")]  // Karnataka
    [InlineData("27AAPFU0939F1ZF")]  // Maharashtra
    [InlineData("07AAACN0192J1ZR")]  // Delhi
    [InlineData("09AAACI1681G1ZL")]  // Uttar Pradesh
    [InlineData("33AABCT1332L1ZZ")]  // Tamil Nadu
    [InlineData("24AAACC4175D1Z4")]  // Gujarat
    public void IsValid_ValidGSTIN_ReturnsTrue(string gstin)
    {
        Assert.True(GstinValidator.IsValid(gstin));
    }

    [Theory]
    [InlineData("29ABCDE1234F1Z5")]
    [InlineData("27AAPFU0939F1ZF")]
    public void GetValidationError_ValidGSTIN_ReturnsNull(string gstin)
    {
        Assert.Null(GstinValidator.GetValidationError(gstin));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Null / empty / whitespace (optional field)
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_NullOrEmpty_ReturnsFalse(string? gstin)
    {
        Assert.False(GstinValidator.IsValid(gstin));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetValidationError_NullOrEmpty_AllowEmpty_ReturnsNull(string? gstin)
    {
        Assert.Null(GstinValidator.GetValidationError(gstin, allowEmpty: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetValidationError_NullOrEmpty_Required_ReturnsError(string? gstin)
    {
        var error = GstinValidator.GetValidationError(gstin, allowEmpty: false);
        Assert.NotNull(error);
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Invalid length
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("29ABCDE1234F1Z")]    // 14 chars
    [InlineData("29ABCDE1234F1Z55")]  // 16 chars
    [InlineData("ABC")]
    public void GetValidationError_WrongLength_ReturnsLengthError(string gstin)
    {
        var error = GstinValidator.GetValidationError(gstin);
        Assert.NotNull(error);
        Assert.Contains("15 characters", error, StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Invalid state code
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("00ABCDE1234F1Z5")]  // 00 not valid
    [InlineData("39ABCDE1234F1Z5")]  // 39 > max 38
    [InlineData("99ABCDE1234F1Z5")]  // Way out of range
    public void GetValidationError_InvalidStateCode_ReturnsStateError(string gstin)
    {
        var error = GstinValidator.GetValidationError(gstin);
        Assert.NotNull(error);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Invalid format (bad PAN section, missing Z, etc.)
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("29ABCD11234F1Z5")]  // PAN section: digit where letter expected
    [InlineData("2912345ABCDF1Z5")]  // PAN section: all wrong
    [InlineData("29ABCDE1234F1A5")]  // Position 14: not 'Z'
    [InlineData("29abcde1234f1z5")]  // Lowercase (IsValid normalizes, but raw format is wrong)
    public void IsValid_InvalidFormat_ReturnsFalse(string gstin)
    {
        // Note: lowercase test — IsValid uppercases internally, so "29abcde1234f1z5" IS valid
        // Only truly malformed patterns should fail
        if (gstin == "29abcde1234f1z5")
        {
            // Lowercase is accepted because IsValid normalizes to upper
            Assert.True(GstinValidator.IsValid(gstin));
            return;
        }

        Assert.False(GstinValidator.IsValid(gstin));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Case insensitivity
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void IsValid_LowercaseGSTIN_IsAccepted()
    {
        // Validator normalizes to uppercase
        Assert.True(GstinValidator.IsValid("29abcde1234f1z5"));
    }

    // ══════════════════════════════════════════════════════════════════
    //  All valid state codes (01–38)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void IsValid_AllStateCodes_01To38_Accepted()
    {
        for (var i = 1; i <= 38; i++)
        {
            var stateCode = i.ToString("D2");
            var gstin = $"{stateCode}ABCDE1234F1Z5";
            Assert.True(GstinValidator.IsValid(gstin),
                $"State code {stateCode} should be valid");
        }
    }

    [Fact]
    public void IsValid_StateCode00_Rejected()
    {
        Assert.False(GstinValidator.IsValid("00ABCDE1234F1Z5"));
    }

    [Fact]
    public void IsValid_StateCode39_Rejected()
    {
        Assert.False(GstinValidator.IsValid("39ABCDE1234F1Z5"));
    }
}
