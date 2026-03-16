using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class IroningBatchItem
{
    public int Id { get; set; }

    public int IroningBatchId { get; set; }

    public IroningBatch? Batch { get; set; }

    [MaxLength(200)]
    public string ClothName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int ReceivedQty { get; set; }

    public decimal Rate { get; set; }

    public decimal Amount { get; set; }
}
