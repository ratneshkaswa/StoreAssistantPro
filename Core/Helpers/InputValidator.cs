namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Centralized input validation rules for the application.
/// All validation methods are pure functions — no side effects, no DI.
/// <para>
/// <b>Architecture rule:</b> ViewModels call these helpers before
/// sending commands. Never duplicate validation logic inline.
/// </para>
/// </summary>
public static class InputValidator
{
    /// <summary>Validate a user PIN (Admin/User): exactly 4 digits.</summary>
    public static bool IsValidUserPin(string pin) =>
        IsDigitsOfLength(pin, 4);

    /// <summary>Validate a master PIN: exactly 6 digits.</summary>
    public static bool IsValidMasterPin(string pin) =>
        IsDigitsOfLength(pin, 6);

    /// <summary>Validate a PIN of a specific length: all digits, exact length.</summary>
    public static bool IsValidPin(string pin, int requiredLength) =>
        IsDigitsOfLength(pin, requiredLength);

    /// <summary>Validate that a required text field is not empty or whitespace.</summary>
    public static bool IsRequired(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    /// <summary>Validate that a decimal value is non-negative.</summary>
    public static bool IsNonNegative(decimal value) =>
        value >= 0;

    /// <summary>Validate that two values match (PIN confirmation, password confirm).</summary>
    public static bool AreEqual(string value, string confirmation) =>
        string.Equals(value, confirmation, StringComparison.Ordinal);

    /// <summary>Validate that all values in a set are distinct.</summary>
    public static bool AreAllDistinct(params string[] values) =>
        values.Distinct(StringComparer.Ordinal).Count() == values.Length;

    private static bool IsDigitsOfLength(string? value, int length) =>
        value is not null && value.Length == length && value.AsSpan().IndexOfAnyExceptInRange('0', '9') < 0;
}
