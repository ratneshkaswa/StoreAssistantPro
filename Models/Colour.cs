using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Predefined colour from a system-managed palette of 100 standard colours.
/// Users cannot add new colours — only the seeded entries are available.
/// Used during inward entry and billing to tag products consistently.
/// </summary>
public class Colour
{
    public int Id { get; set; }

    /// <summary>Colour display name (e.g., "Maroon", "Sky Blue").</summary>
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Hex code for UI display (e.g., "#800000").</summary>
    [Required, MaxLength(7)]
    public string HexCode { get; set; } = string.Empty;

    /// <summary>Sort order for consistent dropdown ordering.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
