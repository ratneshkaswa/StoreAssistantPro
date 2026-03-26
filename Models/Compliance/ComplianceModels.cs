namespace StoreAssistantPro.Models.Compliance;

/// <summary>GST return data (#832-840).</summary>
public sealed class GstReturnData
{
    public string ReturnType { get; set; } = "GSTR1"; // GSTR1, GSTR3B, GSTR9
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CessAmount { get; set; }
    public int InvoiceCount { get; set; }
}

/// <summary>e-Way bill record (#835).</summary>
public sealed class EWayBill
{
    public int Id { get; set; }
    public string EWayBillNumber { get; set; } = string.Empty;
    public int SaleId { get; set; }
    public string TransportMode { get; set; } = "Road";
    public string? VehicleNumber { get; set; }
    public decimal GoodsValue { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime ValidUntil { get; set; }
}

/// <summary>e-Invoice record with IRN (#836).</summary>
public sealed class EInvoice
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public string Irn { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    public string? SignedInvoice { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool IsValid { get; set; } = true;
}

/// <summary>HSN summary entry (#838).</summary>
public sealed record HsnSummaryEntry(
    string HsnCode,
    string? Description,
    decimal TaxableValue,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    int Quantity);

/// <summary>Data retention policy rule (#842).</summary>
public sealed record RetentionPolicy(
    string DataType,
    int RetentionYears,
    bool IsEnabled,
    string? Description);
