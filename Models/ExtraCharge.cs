using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Extra charge applied at the bill level (e.g., packing, delivery, alteration).
/// Multiple extra charges can be applied to a single sale.
/// </summary>
public class ExtraCharge
{
    public int Id { get; set; }

    /// <summary>Name of the charge (e.g., "Packing", "Delivery", "Alteration").</summary>
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Charge amount in currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>Whether this charge is taxable.</summary>
    public bool IsTaxable { get; set; }

    /// <summary>Linked sale.</summary>
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
}
