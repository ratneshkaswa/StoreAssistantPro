using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class IroningBatch
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Outward";

    public decimal PaidAmount { get; set; }

    public DateTime? CompletedDate { get; set; }

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<IroningBatchItem> Items { get; set; } = [];
}
