using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

/// <summary>
/// A unique size + colour combination for a product.
/// Each variant tracks its own stock quantity, barcode, and optional price offset.
/// </summary>
public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int SizeId { get; set; }
    public ProductSize? Size { get; set; }

    public int ColourId { get; set; }
    public Colour? Colour { get; set; }

    /// <summary>Unique barcode for this specific variant (EAN-13 / Code128).</summary>
    [MaxLength(50)]
    public string? Barcode { get; set; }

    /// <summary>Stock quantity for this variant.</summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Price offset from the product's base sale price.
    /// Positive = surcharge (e.g., XXL +₹50), negative = discount.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AdditionalPrice { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Optional image file path for this colour variant (#64).</summary>
    [MaxLength(500)]
    public string? ImagePath { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>Computed display name: "Product — Size / Colour".</summary>
    [NotMapped]
    public string DisplayName => $"{Product?.Name} — {Size?.Name} / {Colour?.Name}";

    [NotMapped]
    public RowHighlightLevel HighlightLevel =>
        !IsActive ? RowHighlightLevel.Inactive
        : Quantity == 0 ? RowHighlightLevel.Warning
        : RowHighlightLevel.None;
}
