namespace StoreAssistantPro.Models.Ecommerce;

/// <summary>E-commerce platform types (#640-685).</summary>
public enum EcommercePlatform { Shopify, WooCommerce, Amazon, Flipkart, Meesho, OwnWebstore }

/// <summary>E-commerce platform connection (#640-653).</summary>
public sealed class PlatformConnection
{
    public int Id { get; set; }
    public EcommercePlatform Platform { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? StoreUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastError { get; set; }
}

/// <summary>Online order pulled from platform (#642, #654).</summary>
public sealed class OnlineOrder
{
    public int Id { get; set; }
    public EcommercePlatform Platform { get; set; }
    public string PlatformOrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? ShippingAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "New"; // New, Processing, Packed, Shipped, Delivered, Cancelled
    public string? TrackingNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime ImportedAt { get; set; }
    public int? ConvertedSaleId { get; set; }
}

/// <summary>Product listing mapping (#648, #667).</summary>
public sealed class ProductListing
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public EcommercePlatform Platform { get; set; }
    public string? PlatformProductId { get; set; }
    public string? PlatformSku { get; set; }
    public decimal? PlatformPrice { get; set; }
    public bool IsListed { get; set; }
    public DateTime? LastSyncAt { get; set; }
}

/// <summary>Marketplace commission tracking (#671).</summary>
public sealed record MarketplaceCommission(
    EcommercePlatform Platform,
    string OrderId,
    decimal SaleAmount,
    decimal CommissionPercent,
    decimal CommissionAmount,
    DateTime OrderDate);
