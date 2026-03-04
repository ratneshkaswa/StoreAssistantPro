namespace StoreAssistantPro.Models;

/// <summary>
/// Maps a <see cref="Product"/> to a <see cref="TaxGroup"/> and <see cref="HSNCode"/>.
/// When <see cref="OverrideAllowed"/> is true, billing operators can override the
/// tax group at the time of sale (useful for promotional or special items).
/// </summary>
public class ProductTaxMapping
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int TaxGroupId { get; set; }
    public TaxGroup? TaxGroup { get; set; }

    public int HSNCodeId { get; set; }
    public HSNCode? HSNCode { get; set; }

    /// <summary>
    /// When true, the operator can override the tax group during billing.
    /// Default is false (locked to the assigned group).
    /// </summary>
    public bool OverrideAllowed { get; set; }

    public DateTime CreatedDate { get; set; }
}
