using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Discount type applied at the bill level.
/// </summary>
public enum DiscountType
{
    /// <summary>No discount applied.</summary>
    None = 0,

    /// <summary>Fixed currency amount off the subtotal (e.g. ₹100 off).</summary>
    Amount = 1,

    /// <summary>Percentage off the subtotal (e.g. 10% off).</summary>
    Percentage = 2
}

/// <summary>
/// Describes an optional discount applied to the entire bill subtotal.
/// <para>
/// This is a value object — it carries intent ("apply 10% off") but
/// does not perform calculation. Calculation is done by
/// <c>IBillCalculationService</c>.
/// </para>
/// <para>
/// <b>Design rule:</b> Discounts are bill-level only.
/// Products carry no discount fields — this keeps the product catalog
/// clean and moves all promotion logic to the billing layer.
/// </para>
/// </summary>
public sealed record BillDiscount
{
    public DiscountType Type { get; init; }

    /// <summary>
    /// The discount value: a flat amount when <see cref="Type"/> is
    /// <see cref="DiscountType.Amount"/>, or a percentage (0–100) when
    /// <see cref="Type"/> is <see cref="DiscountType.Percentage"/>.
    /// Ignored when <see cref="Type"/> is <see cref="DiscountType.None"/>.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Value { get; init; }

    /// <summary>
    /// Optional reason or coupon code for audit trail.
    /// </summary>
    [MaxLength(200)]
    public string? Reason { get; init; }

    /// <summary>No discount.</summary>
    public static BillDiscount None => new() { Type = DiscountType.None, Value = 0m };
}
