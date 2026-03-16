using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public class SalesPurchaseEntry
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime CreatedAt { get; set; }

    [MaxLength(200)]
    public string Note { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [NotMapped]
    public string DisplayAmount => Type == "Sales" ? $"+\u20b9{Amount:N0}" : $"\u2212\u20b9{Amount:N0}";
}
