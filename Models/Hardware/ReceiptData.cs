namespace StoreAssistantPro.Models.Hardware;

/// <summary>Data to print on a thermal receipt.</summary>
public sealed class ReceiptData
{
    public string? StoreName { get; set; }
    public string? StoreAddress { get; set; }
    public string? StorePhone { get; set; }
    public string? StoreGSTIN { get; set; }
    public string? LogoPath { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public IReadOnlyList<ReceiptLineItem> Items { get; set; } = [];
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountPaid { get; set; }
    public decimal ChangeReturned { get; set; }

    public string? FooterText { get; set; }
    public bool PrintBarcode { get; set; } = true;
}

public sealed class ReceiptLineItem
{
    public string ProductName { get; set; } = string.Empty;
    public string? HSNCode { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "pcs";
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}
