using System.Text.RegularExpressions;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Validates Indian GST Identification Number (GSTIN) format.
/// Format: 2-digit state code + 10-char PAN + 1 entity code + 'Z' + 1 checksum.
/// Example: 29ABCDE1234F1Z5
/// </summary>
public static partial class GstinValidator
{
    /// <summary>
    /// Returns <c>true</c> if <paramref name="gstin"/> matches the
    /// 15-character Indian GSTIN pattern.  Null/empty/whitespace returns <c>true</c>
    /// (treat as "not provided" — optional field).
    /// </summary>
    public static bool IsValidOrEmpty(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return true;

        return GstinPattern().IsMatch(gstin.Trim());
    }

    /// <summary>
    /// Returns a user-friendly error message if invalid, or <c>null</c> if valid.
    /// </summary>
    public static string? Validate(string? gstin)
    {
        if (IsValidOrEmpty(gstin))
            return null;

        return "GSTIN must be 15 characters: 2-digit state code + 10-char PAN + entity code + Z + checksum (e.g., 29ABCDE1234F1Z5).";
    }

    /// <summary>
    /// Validates Indian PAN format (10-char alphanumeric: AAAAA9999A).
    /// Null/empty/whitespace returns <c>true</c> (optional field).
    /// </summary>
    public static bool IsPanValidOrEmpty(string? pan)
    {
        if (string.IsNullOrWhiteSpace(pan))
            return true;

        return PanPattern().IsMatch(pan.Trim());
    }

    /// <summary>
    /// Returns a user-friendly error message if PAN is invalid, or <c>null</c> if valid.
    /// </summary>
    public static string? ValidatePan(string? pan)
    {
        if (IsPanValidOrEmpty(pan))
            return null;

        return "PAN must be 10 characters: 5 letters + 4 digits + 1 letter (e.g., ABCDE1234F).";
    }

    // 2 digits (state) + 5 letters + 4 digits + 1 letter + 1 alphanumeric + Z + 1 alphanumeric
    [GeneratedRegex(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][0-9A-Z]Z[0-9A-Z]$", RegexOptions.IgnoreCase)]
    private static partial Regex GstinPattern();

    // 5 letters + 4 digits + 1 letter
    [GeneratedRegex(@"^[A-Z]{5}[0-9]{4}[A-Z]$", RegexOptions.IgnoreCase)]
    private static partial Regex PanPattern();
}
