using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Individual payment leg for a sale. A single sale may have multiple
/// payments (split payment: part cash + part UPI/card). (#118)
/// </summary>
public class SalePayment
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    /// <summary>Payment method for this leg (Cash, UPI, Card, Credit).</summary>
    [Required, MaxLength(50)]
    public string Method { get; set; } = string.Empty;

    /// <summary>Amount paid via this method.</summary>
    public decimal Amount { get; set; }

    /// <summary>Transaction reference for digital payments (UPI ref, card auth code).</summary>
    [MaxLength(100)]
    public string? Reference { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
