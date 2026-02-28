using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Gift voucher issued to customers. Can be redeemed against future purchases.
/// </summary>
public class Voucher
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>Original face value of the voucher.</summary>
    public decimal FaceValue { get; set; }

    /// <summary>Remaining redeemable balance.</summary>
    public decimal Balance { get; set; }

    /// <summary>Customer who owns this voucher (null = bearer voucher).</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public bool IsValid =>
        IsActive
        && Balance > 0
        && DateTime.UtcNow <= ExpiryDate;
}
