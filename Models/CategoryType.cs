using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Top-level grouping for categories (e.g., "Men's Wear", "Women's Wear", "Kids").
/// Categories belong to a CategoryType for two-level product hierarchy.
/// </summary>
public class CategoryType
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<Category> Categories { get; set; } = [];
}
