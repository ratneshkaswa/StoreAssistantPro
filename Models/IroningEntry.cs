using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class IroningEntry
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Items { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Rate { get; set; }

    public decimal Amount { get; set; }

    public bool IsPaid { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
