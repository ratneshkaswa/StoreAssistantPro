using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class BranchBill
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime CreatedAt { get; set; }

    [MaxLength(100)]
    public string BillNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public bool IsCleared { get; set; }

    public DateTime? ClearedAt { get; set; }

    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
