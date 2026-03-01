using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Parent category type for two-level hierarchy.</summary>
    public int? CategoryTypeId { get; set; }
    public CategoryType? CategoryType { get; set; }

    public bool IsActive { get; set; } = true;

    public RowHighlightLevel HighlightLevel =>
        !IsActive ? RowHighlightLevel.Inactive : RowHighlightLevel.None;
}
