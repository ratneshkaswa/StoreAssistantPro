using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public class Sale
{
    public int Id { get; set; }

    /// <summary>
    /// Human-readable invoice number (e.g., INV-20250101-0001).
    /// Generated at sale creation time.
    /// </summary>
    [MaxLength(30)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Optional customer for non-walk-in sales.</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Staff member who made the sale (for incentive tracking).</summary>
    public int? StaffId { get; set; }
    public Staff? Staff { get; set; }

    /// <summary>
    /// Client-generated idempotency key. Prevents the same bill from
    /// being saved twice (e.g. accidental double-click, network retry).
    /// A unique index on this column enforces the constraint at the DB level.
    /// </summary>
    public Guid IdempotencyKey { get; set; }

    [Required, MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// The user type (role) of the cashier who completed this sale.
    /// Populated from <see cref="Core.Session.ISessionService.CurrentUserType"/>
    /// at sale creation time for audit/attribution.
    /// </summary>
    [MaxLength(20)]
    public string? CashierRole { get; set; }

    /// <summary>Discount type applied at bill level.</summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>Discount input value (flat amount or percentage).</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Computed discount amount in currency.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Optional reason or coupon code for audit.</summary>
    [MaxLength(200)]
    public string? DiscountReason { get; set; }


    /// <summary>
    /// Optional payment reference/transaction ID for digital payments.
    /// </summary>
    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<SaleItem> Items { get; set; } = [];

    /// <summary>Payment legs for split payment (#118).</summary>
    public ICollection<SalePayment> Payments { get; set; } = [];

    /// <summary>Extra charges applied to this sale (packing, delivery, etc.).</summary>
    public ICollection<ExtraCharge> ExtraCharges { get; set; } = [];

    /// <summary>Returns processed against this sale.</summary>
    public ICollection<SaleReturn> Returns { get; set; } = [];

    /// <summary>
    /// Summary of item names for tooltip display.
    /// </summary>
    [NotMapped]
    public string ItemsSummary => Items.Count == 0
        ? "No items"
        : string.Join("\n", Items.Select(i => $"{i.Product?.Name ?? "?"} ×{i.Quantity}"));

    /// <summary>
    /// Human-readable discount summary for detail panel display.
    /// </summary>
    [NotMapped]
    public string DiscountSummary => DiscountType == DiscountType.None
        ? string.Empty
        : $"{DiscountType}: {DiscountValue}{(DiscountType == DiscountType.Percentage ? "%" : "")} = −{DiscountAmount:C}{(string.IsNullOrEmpty(DiscountReason) ? "" : $" ({DiscountReason})")}";
}
