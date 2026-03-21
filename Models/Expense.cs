using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Expense
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime CreatedAt { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>Payment method used: Cash, UPI, Card, Bank Transfer (#225).</summary>
    [MaxLength(30)]
    public string PaymentMethod { get; set; } = "Cash";

    /// <summary>Optional description or notes for the expense (#225).</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>User/role who created the expense (#225).</summary>
    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
