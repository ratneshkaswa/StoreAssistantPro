using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public enum PurchaseOrderStatus
{
    Draft = 0,
    Ordered = 1,
    PartialReceived = 2,
    Received = 3,
    Cancelled = 4
}

public class PurchaseOrder
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    /// <summary>
    /// Computed total: Σ (Quantity × UnitCost) for all items.
    /// </summary>
    public decimal TotalAmount => Items.Sum(i => i.Quantity * i.UnitCost);

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}

public class PurchaseOrderItem
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    public int QuantityReceived { get; set; }

    public decimal UnitCost { get; set; }

    public decimal Subtotal => Quantity * UnitCost;
}
