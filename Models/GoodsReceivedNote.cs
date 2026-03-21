using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public enum GRNStatus
{
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}

public class GoodsReceivedNote
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string GRNNumber { get; set; } = string.Empty;

    public DateTime ReceivedDate { get; set; }

    /// <summary>Optional link to purchase order (#363).</summary>
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public GRNStatus Status { get; set; } = GRNStatus.Draft;

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Persisted total: Σ (QtyReceived × UnitCost).</summary>
    public decimal TotalAmount { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<GRNItem> Items { get; set; } = [];
}

public class GRNItem
{
    public int Id { get; set; }

    public int GRNId { get; set; }
    public GoodsReceivedNote? GRN { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int QtyExpected { get; set; }
    public int QtyReceived { get; set; }
    public int QtyRejected { get; set; }

    public decimal UnitCost { get; set; }

    [NotMapped]
    public decimal Subtotal => QtyReceived * UnitCost;
}
