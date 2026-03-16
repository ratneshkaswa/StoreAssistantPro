using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class PettyCashDeposit
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime CreatedAt { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
