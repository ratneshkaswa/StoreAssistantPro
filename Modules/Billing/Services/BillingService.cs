using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public class BillingService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : IBillingService
{
    public async Task<Sale> CompleteSaleAsync(CompleteSaleDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Items.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        using var _ = perf.BeginScope("BillingService.CompleteSaleAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            // Idempotency guard
            if (await context.Sales.AnyAsync(s => s.IdempotencyKey == dto.IdempotencyKey, ct).ConfigureAwait(false))
                throw new InvalidOperationException("This sale has already been recorded (duplicate submission).");

            var invoiceNumber = await GenerateNextInvoiceAsync(context, ct);

            // Calculate totals
            var items = new List<SaleItem>();
            decimal subtotal = 0;

            foreach (var ci in dto.Items)
            {
                var discountedPrice = ci.UnitPrice * (1 - ci.ItemDiscountRate / 100m);
                var lineTotal = ci.Quantity * discountedPrice;
                subtotal += lineTotal;

                items.Add(new SaleItem
                {
                    ProductId = ci.ProductId,
                    ProductVariantId = ci.ProductVariantId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    ItemDiscountRate = ci.ItemDiscountRate
                });

                // Deduct stock
                if (ci.ProductVariantId.HasValue)
                {
                    var variant = await context.ProductVariants
                        .FirstOrDefaultAsync(v => v.Id == ci.ProductVariantId.Value, ct)
                        .ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"Variant Id {ci.ProductVariantId} not found.");
                    if (variant.Quantity < ci.Quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock for variant {ci.ProductVariantId}. Available: {variant.Quantity}, requested: {ci.Quantity}.");
                    variant.Quantity -= ci.Quantity;
                }
                else
                {
                    var product = await context.Products
                        .FirstOrDefaultAsync(p => p.Id == ci.ProductId, ct)
                        .ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"Product Id {ci.ProductId} not found.");
                    if (product.Quantity < ci.Quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock for product '{product.Name}'. Available: {product.Quantity}, requested: {ci.Quantity}.");
                    product.Quantity -= ci.Quantity;
                }
            }

            // Bill-level discount
            decimal discountAmount = dto.DiscountType switch
            {
                DiscountType.Amount => dto.DiscountValue,
                DiscountType.Percentage => subtotal * dto.DiscountValue / 100m,
                _ => 0
            };
            discountAmount = Math.Min(discountAmount, subtotal);

            var sale = new Sale
            {
                InvoiceNumber = invoiceNumber,
                SaleDate = regional.Now,
                TotalAmount = subtotal - discountAmount,
                PaymentMethod = dto.PaymentMethod,
                PaymentReference = dto.PaymentReference,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                DiscountAmount = discountAmount,
                DiscountReason = dto.DiscountReason,
                CashierRole = dto.CashierRole,
                IdempotencyKey = dto.IdempotencyKey,
                Items = items
            };

            context.Sales.Add(sale);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            return sale;
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingService.GenerateInvoiceNumberAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await GenerateNextInvoiceAsync(context, ct);
    }

    public async Task<ProductLookupResult?> LookupByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        using var _ = perf.BeginScope("BillingService.LookupByBarcodeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = barcode.Trim();

        // Try variant barcode first
        var variant = await context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.Size)
            .Include(v => v.Colour)
            .FirstOrDefaultAsync(v => v.Barcode == trimmed && v.IsActive, ct)
            .ConfigureAwait(false);

        if (variant?.Product is not null)
            return new ProductLookupResult(variant.Product, variant);

        // Then try product-level barcode
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Tax)
            .FirstOrDefaultAsync(p => p.Barcode == trimmed && p.IsActive, ct)
            .ConfigureAwait(false);

        return product is not null ? new ProductLookupResult(product, null) : null;
    }

    public async Task<IReadOnlyList<Product>> SearchProductsAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        using var _ = perf.BeginScope("BillingService.SearchProductsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var term = query.Trim();
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Tax)
            .Where(p => p.IsActive && p.Name.Contains(term))
            .OrderBy(p => p.Name)
            .Take(20)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<string> GenerateNextInvoiceAsync(AppDbContext context, CancellationToken ct)
    {
        var today = regional.Now.Date;
        var prefix = $"INV-{today:yyyyMMdd}-";

        var lastInvoice = await context.Sales
            .Where(s => s.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(s => s.InvoiceNumber)
            .Select(s => s.InvoiceNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var nextSeq = 1;
        if (lastInvoice is not null)
        {
            var seqPart = lastInvoice[prefix.Length..];
            if (int.TryParse(seqPart, out var seq))
                nextSeq = seq + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }
}
