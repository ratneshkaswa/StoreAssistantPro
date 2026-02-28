using System.Text.RegularExpressions;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Indian GST Identification Number (GSTIN) validation.
/// <para>
/// <b>Format (15 characters):</b>
/// <code>
/// SS PPPPPPPPPP E Z C
/// ││ ││││││││││ │ │ └─ Check digit (alphanumeric, Luhn mod-36)
/// ││ ││││││││││ │ └─── Default 'Z' (reserved, always 'Z' currently)
/// ││ ││││││││││ └───── Entity code (1-9 or A-Z)
/// ││ └┘┘┘┘┘┘┘┘──────── PAN (10 alphanumeric: 5 letters, 4 digits, 1 letter)
/// └┘──────────────────── State code (01–38)
/// </code>
/// </para>
/// <para>
/// <b>Examples:</b>
/// <list type="bullet">
///   <item><c>29ABCDE1234F1Z5</c> — Karnataka (29)</item>
///   <item><c>27AAPFU0939F1ZF</c> — Maharashtra (27)</item>
///   <item><c>07AAACN0192J1ZR</c> — Delhi (07)</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// if (!GstinValidator.IsValid(gstin))
///     ShowError("Invalid GSTIN format.");
///
/// string? error = GstinValidator.GetValidationError(gstin);
/// </code>
/// </para>
/// </summary>
public static partial class GstinValidator
{
    /// <summary>
    /// Full GSTIN pattern:
    /// - Positions 1-2: State code (01-38)
    /// - Positions 3-7: 5 uppercase letters (PAN part 1)
    /// - Positions 8-11: 4 digits (PAN part 2)
    /// - Position 12: 1 uppercase letter (PAN part 3)
    /// - Position 13: Entity number (1-9 or A-Z)
    /// - Position 14: 'Z' (reserved)
    /// - Position 15: Check digit (alphanumeric)
    /// </summary>
    [GeneratedRegex(
        @"^(0[1-9]|[12]\d|3[0-8])[A-Z]{5}\d{4}[A-Z][A-Z\d]Z[A-Z\d]$")]
    private static partial Regex GstinRegex();

    /// <summary>
    /// Valid Indian state/UT codes (01–38).
    /// </summary>
    private static readonly HashSet<string> ValidStateCodes =
    [
        "01", "02", "03", "04", "05", "06", "07", "08", "09", "10",
        "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
        "21", "22", "23", "24", "25", "26", "27", "28", "29", "30",
        "31", "32", "33", "34", "35", "36", "37", "38"
    ];

    /// <summary>
    /// Returns <c>true</c> if <paramref name="gstin"/> is a valid
    /// 15-character Indian GSTIN. Null/empty/whitespace returns <c>false</c>.
    /// </summary>
    public static bool IsValid(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return false;

        var trimmed = gstin.Trim().ToUpperInvariant();
        return trimmed.Length == 15
               && GstinRegex().IsMatch(trimmed)
               && ValidStateCodes.Contains(trimmed[..2]);
    }

    /// <summary>
    /// Returns a user-friendly error message if <paramref name="gstin"/>
    /// is invalid, or <c>null</c> if it's valid.
    /// <para>
    /// When <paramref name="gstin"/> is null/empty and
    /// <paramref name="allowEmpty"/> is <c>true</c>, returns <c>null</c>
    /// (GSTIN is optional for some entities).
    /// </para>
    /// </summary>
    public static string? GetValidationError(string? gstin, bool allowEmpty = true)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return allowEmpty ? null : "GSTIN is required.";

        var trimmed = gstin.Trim().ToUpperInvariant();

        if (trimmed.Length != 15)
            return "GSTIN must be exactly 15 characters.";

        if (!char.IsDigit(trimmed[0]) || !char.IsDigit(trimmed[1]))
            return "GSTIN must start with a 2-digit state code.";

        var stateCode = trimmed[..2];
        if (!ValidStateCodes.Contains(stateCode))
            return $"Invalid state code '{stateCode}'. Must be 01–38.";

        if (!GstinRegex().IsMatch(trimmed))
            return "Invalid GSTIN format. Expected: 2-digit state + 10-char PAN + entity + Z + check digit.";

        return null;
    }
}
