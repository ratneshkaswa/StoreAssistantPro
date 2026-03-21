using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Payment made to a vendor/supplier (#90).
/// Part of the supplier ledger (#87) which tracks running balance.
/// </summary>
public class VendorPayment
{
    public int Id { get; set; }

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    /// <summary>Amount paid (₹).</summary>
    public decimal Amount { get; set; }

    /// <summary>Payment method (Cash, UPI, Bank Transfer, Cheque).</summary>
    [Required, MaxLength(30)]
    public string PaymentMethod { get; set; } = "Cash";

    /// <summary>Reference (cheque number, UPI ref, bank ref).</summary>
    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public DateTime PaymentDate { get; set; }

    /// <summary>Who recorded this payment.</summary>
    public int UserId { get; set; }
}
