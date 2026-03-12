using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Shared Indian business-profile rules used by setup and firm management.
/// Keep these helpers pure so validation stays consistent across the app.
/// </summary>
public static partial class BusinessProfileRules
{
    private static readonly (string Code, string Name)[] CanonicalIndianStates =
    [
        ("37", "Andhra Pradesh"),
        ("12", "Arunachal Pradesh"),
        ("18", "Assam"),
        ("10", "Bihar"),
        ("22", "Chhattisgarh"),
        ("30", "Goa"),
        ("24", "Gujarat"),
        ("06", "Haryana"),
        ("02", "Himachal Pradesh"),
        ("20", "Jharkhand"),
        ("29", "Karnataka"),
        ("32", "Kerala"),
        ("23", "Madhya Pradesh"),
        ("27", "Maharashtra"),
        ("14", "Manipur"),
        ("17", "Meghalaya"),
        ("15", "Mizoram"),
        ("13", "Nagaland"),
        ("21", "Odisha"),
        ("03", "Punjab"),
        ("08", "Rajasthan"),
        ("11", "Sikkim"),
        ("33", "Tamil Nadu"),
        ("36", "Telangana"),
        ("16", "Tripura"),
        ("09", "Uttar Pradesh"),
        ("05", "Uttarakhand"),
        ("19", "West Bengal"),
        ("35", "Andaman & Nicobar Islands"),
        ("04", "Chandigarh"),
        ("26", "Dadra & Nagar Haveli and Daman & Diu"),
        ("07", "Delhi"),
        ("01", "Jammu & Kashmir"),
        ("38", "Ladakh"),
        ("31", "Lakshadweep"),
        ("34", "Puducherry")
    ];

    private static readonly IReadOnlyDictionary<string, string> _stateNameByCode =
        CanonicalIndianStates.ToDictionary(state => state.Code, state => state.Name, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> _stateCodeByName = BuildStateCodeByName();

    private static readonly IReadOnlyList<string> _stateNames =
        CanonicalIndianStates.Select(state => state.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IReadOnlyList<(string Code, string Name)> IndianStateData => CanonicalIndianStates;

    public static IReadOnlyDictionary<string, string> IndianStateNameByCode => _stateNameByCode;

    public static IReadOnlyDictionary<string, string> IndianStateCodeByName => _stateCodeByName;

    public static IReadOnlyList<string> IndianStateNames => _stateNames;

    public static bool IsValidIndianState(string? stateName) =>
        string.IsNullOrWhiteSpace(stateName) || _stateCodeByName.ContainsKey(stateName.Trim());

    public static string? GetStateNameByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return _stateNameByCode.TryGetValue(code.Trim(), out var stateName)
            ? stateName
            : null;
    }

    public static string? GetStateCodeFromName(string? stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
            return null;

        return _stateCodeByName.TryGetValue(stateName.Trim(), out var code)
            ? code
            : null;
    }

    public static string? GetCanonicalStateName(string? stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
            return null;

        var code = GetStateCodeFromName(stateName);
        return code is null
            ? stateName?.Trim()
            : GetStateNameByCode(code);
    }

    public static string? ExtractKnownGstinStateCode(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return null;

        var normalized = gstin.Trim().ToUpperInvariant();
        if (normalized.Length < 2)
            return null;

        var code = normalized[..2];
        return _stateNameByCode.ContainsKey(code) ? code : null;
    }

    public static string? GetStateCodeFromGstinOrState(string? gstin, string? stateName) =>
        ExtractKnownGstinStateCode(gstin) ?? GetStateCodeFromName(stateName);

    public static bool IsGstinStateConsistent(string? gstin, string? stateName)
    {
        var derivedCode = ExtractKnownGstinStateCode(gstin);
        if (derivedCode is null || string.IsNullOrWhiteSpace(stateName))
            return true;

        var derivedState = GetStateNameByCode(derivedCode);
        return derivedState is null
            || string.Equals(derivedState, stateName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var trimmed = phone.Trim();
        return trimmed.Length == 10 && trimmed.AsSpan().IndexOfAnyExceptInRange('0', '9') < 0;
    }

    public static bool TryParseCompositionRate(string? value, out decimal rate)
    {
        rate = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        var normalized = trimmed.Replace(" ", string.Empty);

        if (normalized.Contains(',') && !normalized.Contains('.'))
        {
            var commaCount = normalized.Count(c => c == ',');
            if (commaCount == 1)
            {
                var commaIndex = normalized.IndexOf(',');
                var decimals = normalized.Length - commaIndex - 1;
                normalized = decimals is >= 1 and <= 2
                    ? normalized.Replace(',', '.')
                    : normalized.Replace(",", string.Empty);
            }
            else
            {
                normalized = normalized.Replace(",", string.Empty);
            }
        }
        else
        {
            normalized = normalized.Replace(",", string.Empty);
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out rate);
    }

    public static bool IsValidCompositionRate(string? value) =>
        TryParseCompositionRate(value, out var rate) && rate is >= 0 and <= 100;

    public static bool IsValidGstin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var gstin = value.Trim().ToUpperInvariant();
        return GstinRegex().IsMatch(gstin) && VerifyGstinChecksum(gstin);
    }

    public static bool IsValidPan(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return PanRegex().IsMatch(value.Trim().ToUpperInvariant());
    }

    public static bool VerifyGstinChecksum(string gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin) || gstin.Length != 15)
            return false;

        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sum = 0;

        for (var i = 0; i < 14; i++)
        {
            var index = chars.IndexOf(char.ToUpperInvariant(gstin[i]));
            if (index < 0)
                return false;

            var factor = i % 2 == 0 ? 1 : 2;
            var product = index * factor;
            sum += product / 36 + product % 36;
        }

        var checkIndex = (36 - sum % 36) % 36;
        return char.ToUpperInvariant(gstin[14]) == chars[checkIndex];
    }

    public static ValidationResult? ValidateIndianState(object? value, ValidationContext context)
    {
        var state = value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(state))
            return ValidationResult.Success;

        if (!IsValidIndianState(state))
            return new ValidationResult("Please select a valid Indian state from the list.");

        var gstin = ReadStringProperty(context.ObjectInstance, "GSTNumber")
            ?? ReadStringProperty(context.ObjectInstance, "GSTIN");

        return IsGstinStateConsistent(gstin, state)
            ? ValidationResult.Success
            : new ValidationResult("GSTIN state code does not match selected state.");
    }

    public static ValidationResult? ValidateGstin(object? value, ValidationContext context)
    {
        var gstin = value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(gstin))
            return ValidationResult.Success;

        var normalized = gstin.Trim().ToUpperInvariant();
        if (!GstinRegex().IsMatch(normalized))
            return new ValidationResult("Invalid GSTIN format.");

        if (!VerifyGstinChecksum(normalized))
            return new ValidationResult("GSTIN check digit is invalid.");

        var state = ReadStringProperty(context.ObjectInstance, "State");
        return IsGstinStateConsistent(normalized, state)
            ? ValidationResult.Success
            : new ValidationResult("GSTIN state code does not match selected state.");
    }

    public static ValidationResult? ValidatePan(object? value, ValidationContext context)
    {
        var pan = value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(pan))
            return ValidationResult.Success;

        return PanRegex().IsMatch(pan.Trim().ToUpperInvariant())
            ? ValidationResult.Success
            : new ValidationResult("Invalid PAN format (e.g. ABCDE1234F).");
    }

    public static ValidationResult? ValidateCompositionRate(object? value, ValidationContext context)
    {
        if (!IsCompositionScheme(context.ObjectInstance))
            return ValidationResult.Success;

        var rateText = value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rateText))
            return new ValidationResult("Composition rate is required for composition GST.");

        if (!TryParseCompositionRate(rateText, out _))
            return new ValidationResult("Composition rate must be a valid number (0-100).");

        return IsValidCompositionRate(rateText)
            ? ValidationResult.Success
            : new ValidationResult("Composition rate must be between 0 and 100.");
    }

    private static IReadOnlyDictionary<string, string> BuildStateCodeByName()
    {
        var map = CanonicalIndianStates.ToDictionary(
            state => state.Name,
            state => state.Code,
            StringComparer.OrdinalIgnoreCase);

        map["Andaman & Nicobar"] = "35";
        map["Dadra & Nagar Haveli"] = "26";

        return map;
    }

    private static string? ReadStringProperty(object objectInstance, string propertyName)
    {
        var property = objectInstance.GetType().GetProperty(propertyName);
        return property?.GetValue(objectInstance) as string;
    }

    private static bool IsCompositionScheme(object objectInstance)
    {
        var compositionFlag = objectInstance.GetType().GetProperty("IsCompositionScheme")?.GetValue(objectInstance);
        if (compositionFlag is bool isCompositionScheme)
            return isCompositionScheme;

        var registrationType = ReadStringProperty(objectInstance, "SelectedGstRegistrationType");
        return string.Equals(registrationType, "Composition", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^\d{2}[A-Z]{5}\d{4}[A-Z]\d[A-Z][A-Z\d]$")]
    private static partial Regex GstinRegex();

    [GeneratedRegex(@"^[A-Z]{5}\d{4}[A-Z]$")]
    private static partial Regex PanRegex();
}
