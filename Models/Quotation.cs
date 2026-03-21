using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public enum QuotationStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    ConvertedToSale = 5
}

public class Quotation
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string QuoteNumber { get; set; } = string.Empty;

    public DateTime QuoteDate { get; set; }

    /// <summary>Validity end date. System auto-marks as Expired after this.</summary>
    public DateTime ValidUntil { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    /// <summary>Optional customer — walk-in quotations have null.</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Sale created when quotation is converted (#351).</summary>
    public int? ConvertedSaleId { get; set; }
    public Sale? ConvertedSale { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Persisted total: Σ line totals.</summary>
    public decimal TotalAmount { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<QuotationItem> Items { get; set; } = [];
}

public class QuotationItem
{
    public int Id { get; set; }

    public int QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>Item-level discount rate (0–100 %).</summary>
    [Range(0, 100)]
    public decimal DiscountRate { get; set; }

    /// <summary>Tax rate snapshot (GST %).</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Computed tax amount for this line.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Cess rate snapshot (%).</summary>
    public decimal CessRate { get; set; }

    /// <summary>Computed cess amount for this line.</summary>
    public decimal CessAmount { get; set; }

    [NotMapped]
    public decimal DiscountAmount => UnitPrice * DiscountRate / 100m;

    [NotMapped]
    public decimal DiscountedPrice => UnitPrice - DiscountAmount;

    [NotMapped]
    public decimal Subtotal => Quantity * DiscountedPrice;

    [NotMapped]
    public decimal LineTotal => Subtotal + TaxAmount + CessAmount;
}
