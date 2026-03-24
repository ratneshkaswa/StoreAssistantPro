using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Promotional discount rules that auto-apply at billing time (#180-186, #189-190).
/// </summary>
public class DiscountRule
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule type: BuyXGetY, ComboBundle, Seasonal, Category, Brand,
    /// CustomerSpecific, CouponCode, MinBillDiscount.
    /// </summary>
    [Required, MaxLength(30)]
    public string RuleType { get; set; } = string.Empty;

    /// <summary>Discount type: Flat or Percentage.</summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>Discount value (₹ or %).</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Minimum bill amount required (#189).</summary>
    public decimal MinBillAmount { get; set; }

    /// <summary>Maximum discount cap (for percentage rules).</summary>
    public decimal? MaxDiscountAmount { get; set; }

    // ── Buy X Get Y (#180) ──

    /// <summary>Buy this many to trigger the rule.</summary>
    public int BuyQuantity { get; set; }

    /// <summary>Get this many free items.</summary>
    public int FreeQuantity { get; set; }

    // ── Category/Brand targeting (#183/#184) ──

    /// <summary>Applies to products in this category (null = all).</summary>
    public int? CategoryId { get; set; }

    /// <summary>Applies to products of this brand (null = all).</summary>
    public int? BrandId { get; set; }

    // ── Customer-specific (#185) ──

    /// <summary>Applies only to this customer (null = all customers).</summary>
    public int? CustomerId { get; set; }

    // ── Combo/bundle (#181) ──

    /// <summary>Comma-separated product IDs for combo pricing.</summary>
    [MaxLength(500)]
    public string? ComboProductIds { get; set; }

    /// <summary>Special combo price when all products are bought together.</summary>
    public decimal? ComboPrice { get; set; }

    // ── Validity (#182) ──

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    // ── Stacking (#190) ──

    /// <summary>Whether this discount can stack with other discounts.</summary>
    public bool AllowStacking { get; set; }

    /// <summary>Priority for non-stacking conflicts (higher = applied first).</summary>
    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }
}
