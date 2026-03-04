using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// User-managed size for clothing products (e.g., "S", "M", "L", "XL", "28", "30", "Free Size").
/// Manual entry — users can add new sizes as needed.
/// </summary>
public class ProductSize
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Sort order for consistent dropdown ordering (S → M → L → XL).</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
