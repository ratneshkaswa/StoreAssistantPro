using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// A product line item within an <see cref="InwardParcel"/>.
/// Captures the product, quantity, and attribute selections
/// (colour, size, pattern, type) based on the product's attribute flags.
/// Maximum 3 products per parcel.
/// </summary>
public class InwardProduct
{
    public int Id { get; set; }

    public int InwardParcelId { get; set; }
    public InwardParcel? InwardParcel { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>
    /// Quantity received. Unit depends on <see cref="Product.Unit"/>:
    /// pieces for readymade, meters for garment cloth.
    /// </summary>
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Selected colour (FK to <see cref="Colour"/>).
    /// Only populated when <see cref="Product.SupportsColour"/> is true.
    /// </summary>
    public int? ColourId { get; set; }
    public Colour? Colour { get; set; }

    /// <summary>
    /// Selected size (FK to <see cref="ProductSize"/>).
    /// Only populated when <see cref="Product.SupportsSize"/> is true.
    /// </summary>
    public int? SizeId { get; set; }
    public ProductSize? Size { get; set; }

    /// <summary>
    /// Selected pattern (FK to <see cref="ProductPattern"/>).
    /// Only populated when <see cref="Product.SupportsPattern"/> is true.
    /// </summary>
    public int? PatternId { get; set; }
    public ProductPattern? Pattern { get; set; }

    /// <summary>
    /// Selected type/variant (FK to <see cref="ProductVariantType"/>).
    /// Only populated when <see cref="Product.SupportsType"/> is true.
    /// </summary>
    public int? VariantTypeId { get; set; }
    public ProductVariantType? VariantType { get; set; }
}
