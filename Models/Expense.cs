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

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
