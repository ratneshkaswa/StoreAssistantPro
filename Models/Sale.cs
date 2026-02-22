using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Sale
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Client-generated idempotency key. Prevents the same bill from
    /// being saved twice (e.g. accidental double-click, network retry).
    /// A unique index on this column enforces the constraint at the DB level.
    /// </summary>
    public Guid IdempotencyKey { get; set; }

    [Required, MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Discount type applied at bill level.</summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>Discount input value (flat amount or percentage).</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Computed discount amount in currency.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Optional reason or coupon code for audit.</summary>
    [MaxLength(200)]
    public string? DiscountReason { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<SaleItem> Items { get; set; } = [];
}
