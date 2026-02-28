using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Supplier
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    [MaxLength(15)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    /// <summary>
    /// GST Identification Number (15-char alphanumeric for India).
    /// </summary>
    [MaxLength(15)]
    public string? GSTIN { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
