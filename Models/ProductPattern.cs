using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// User-managed pattern for clothing products (e.g., "Checks", "Stripes", "Plain", "Printed").
/// Manual entry — users can add new patterns as needed.
/// </summary>
public class ProductPattern
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
