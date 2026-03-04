using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// User-managed type/variant for clothing products (e.g., "Half Sleeve", "Full Sleeve", "Slim Fit", "Regular Fit").
/// Manual entry — users can add new types as needed.
/// </summary>
public class ProductVariantType
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
