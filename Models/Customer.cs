using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Customer master record for CRM and billing attribution.
/// Walk-in sales use <c>CustomerId = null</c> on <see cref="Sale"/>.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>GSTIN for B2B invoicing (15-char Indian format).</summary>
    [MaxLength(15)]
    public string? GSTIN { get; set; }

    /// <summary>Loyalty points balance.</summary>
    public int LoyaltyPoints { get; set; }

    /// <summary>Total purchase amount (lifetime).</summary>
    public decimal TotalPurchaseAmount { get; set; }

    /// <summary>Total number of visits/bills.</summary>
    public int VisitCount { get; set; }

    /// <summary>Customer birthday for discount campaigns (#164).</summary>
    public DateTime? Birthday { get; set; }

    /// <summary>Customer anniversary for promotions (#165).</summary>
    public DateTime? Anniversary { get; set; }

    /// <summary>Auto-computed tier: Regular, Silver, Gold, Platinum (#163).</summary>
    [MaxLength(20)]
    public string Tier { get; set; } = "Regular";

    /// <summary>Group/tag for segmentation: Wholesale, Regular, VIP (#172).</summary>
    [MaxLength(50)]
    public string? CustomerGroup { get; set; }

    /// <summary>Maximum outstanding credit balance allowed. 0 = unlimited (#173).</summary>
    public decimal CreditLimit { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
