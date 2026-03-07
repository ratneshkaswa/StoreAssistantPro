using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

/// <summary>
/// Discount coupon that can be applied at billing time.
/// Coupons have a code, value, validity period, and usage limits.
/// </summary>
public class Coupon
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>Discount type: flat amount or percentage.</summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>Discount value (amount in ₹ or percentage 0–100).</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Minimum bill amount required to apply this coupon.</summary>
    public decimal MinBillAmount { get; set; }

    /// <summary>Maximum discount cap (for percentage coupons).</summary>
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>Total number of times this coupon can be used. 0 = unlimited.</summary>
    public int MaxUsageCount { get; set; }

    /// <summary>Number of times this coupon has been used.</summary>
    public int UsedCount { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Whether the coupon is currently usable.
    /// Note: ValidFrom/ValidTo are stored in local time (IST) by the service layer.
    /// </summary>
    [NotMapped]
    public bool IsValid =>
        IsActive
        && DateTime.Now >= ValidFrom
        && DateTime.Now <= ValidTo
        && (MaxUsageCount == 0 || UsedCount < MaxUsageCount);
}
