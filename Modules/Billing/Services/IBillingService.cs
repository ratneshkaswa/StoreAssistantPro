using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public interface IBillingService
{
    /// <summary>Create a new sale with items and deduct stock atomically.</summary>
    Task<Sale> CompleteSaleAsync(CompleteSaleDto dto, CancellationToken ct = default);

    /// <summary>Generate the next sequential invoice number.</summary>
    Task<string> GenerateInvoiceNumberAsync(CancellationToken ct = default);

    /// <summary>Look up a product by barcode (product-level or variant barcode).</summary>
    Task<ProductLookupResult?> LookupByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>Search products by name for type-ahead.</summary>
    Task<IReadOnlyList<Product>> SearchProductsAsync(string query, CancellationToken ct = default);
}

public record CompleteSaleDto(
    IReadOnlyList<CartItemDto> Items,
    string PaymentMethod,
    string? PaymentReference,
    DiscountType DiscountType,
    decimal DiscountValue,
    string? DiscountReason,
    decimal CashTendered,
    string CashierRole,
    Guid IdempotencyKey,
    int? CustomerId,
    string? DiscountApprovalPin = null);

public record CartItemDto(
    int ProductId,
    int? ProductVariantId,
    int Quantity,
    decimal UnitPrice,
    decimal ItemDiscountRate,
    decimal ItemDiscountAmount,
    decimal TaxRate,
    bool IsTaxInclusive,
    decimal TaxAmount);

/// <summary>Result of barcode lookup — could be product-level or variant-level.</summary>
public record ProductLookupResult(
    Product Product,
    ProductVariant? Variant);
