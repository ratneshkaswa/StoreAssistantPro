using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public class Brand
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When <c>false</c>, the brand is hidden from product assignment.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];

    /// <summary>
    /// Precomputed count of products in this brand. Populated by service queries.
    /// </summary>
    [NotMapped]
    public int ProductCount { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public RowHighlightLevel HighlightLevel =>
        !IsActive ? RowHighlightLevel.Inactive : RowHighlightLevel.None;
}
