using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public class Order
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime CreatedAt { get; set; }

    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ItemDescription { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Rate { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime? DeliveryDate { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [NotMapped]
    public bool IsOverdue => DeliveryDate.HasValue
        && DeliveryDate.Value.Date < DateTime.Today
        && Status != "Delivered"
        && Status != "Cancelled";

    [NotMapped]
    public string EntryTimestamp => CreatedAt.Year > 1
        ? CreatedAt.ToString("dd-MMM  hh:mm tt")
        : Date.ToString("dd-MMM  hh:mm tt");
}
