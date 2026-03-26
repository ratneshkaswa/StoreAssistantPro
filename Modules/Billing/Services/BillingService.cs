using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Modules.Billing.Services;

public class BillingService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILoginService loginService,
    IAuditService auditService,
    ICashRegisterService cashRegisterService,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : IBillingService
{
    public async Task<decimal> GetMaxDiscountPercentAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return config?.MaxDiscountPercent ?? 0;
    }

    public async Task<Sale> CompleteSaleAsync(CompleteSaleDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Items.Count == 0)
            throw new InvalidOperationException("Cart is empty.");
        if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
            throw new InvalidOperationException("Payment method is required.");

        // Split payment validation (#118)
        var isSplit = dto.SplitPayments is { Count: > 0 };
        if (isSplit)
        {
            var splitTotal = dto.SplitPayments!.Sum(p => p.Amount);
            foreach (var leg in dto.SplitPayments!)
            {
                if (string.IsNullOrWhiteSpace(leg.Method))
                    throw new InvalidOperationException("Each split payment leg must have a method.");
                if (leg.Amount <= 0)
                    throw new InvalidOperationException("Each split payment leg amount must be positive.");
                if (!string.Equals(leg.Method, "Cash", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(leg.Method, "Credit", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(leg.Reference))
                    throw new InvalidOperationException($"Payment reference required for {leg.Method} payment.");
            }
        }
        else if (!string.Equals(dto.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(dto.PaymentMethod, "Credit", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(dto.PaymentReference))
        {
            throw new InvalidOperationException("Payment reference is required for non-cash sales.");
        }
        if (dto.DiscountValue < 0)
            throw new InvalidOperationException("Discount value cannot be negative.");
        if (dto.DiscountType == DiscountType.Percentage && dto.DiscountValue > 100)
            throw new InvalidOperationException("Discount percentage must be between 0 and 100.");
        if (dto.Items.Any(item => item.Quantity <= 0))
            throw new InvalidOperationException("Cart item quantity must be greater than zero.");
        if (dto.Items.Any(item => item.UnitPrice < 0))
            throw new InvalidOperationException("Cart item price cannot be negative.");
        if (dto.Items.Any(item => item.ItemDiscountRate is < 0 or > 100))
            throw new InvalidOperationException("Cart item discount must be between 0 and 100.");

        // Day close lockdown (#246): block sales after register is closed for the day
        if (await cashRegisterService.IsDayClosedAsync(regional.Now, ct).ConfigureAwait(false))
            throw new InvalidOperationException("Business day is closed. No more sales allowed today.");

        using var scope = perf.BeginScope("BillingService.CompleteSaleAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Max discount limit (#179) + approval PIN (#178)
        var subtotalForValidation = dto.Items.Sum(i =>
            i.Quantity * i.UnitPrice * (1 - i.ItemDiscountRate / 100m));
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (config is not null && config.MaxDiscountPercent > 0
            && dto.DiscountType != DiscountType.None && dto.DiscountValue > 0)
        {
            var effectivePct = dto.DiscountType == DiscountType.Percentage
                ? dto.DiscountValue
                : subtotalForValidation > 0 ? dto.DiscountValue / subtotalForValidation * 100m : 0;

            if (effectivePct > config.MaxDiscountPercent)
            {
                if (string.IsNullOrWhiteSpace(dto.DiscountApprovalPin))
                    throw new InvalidOperationException(
                        $"Discount exceeds {config.MaxDiscountPercent}%. Manager PIN required.");

                var pinValid = await loginService.ValidateMasterPinAsync(dto.DiscountApprovalPin, ct);
                if (!pinValid)
                    throw new InvalidOperationException("Invalid discount approval PIN.");
            }
        }

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

            var priceOverrides = new List<(int ProductId, string ProductName, decimal OldPrice, decimal NewPrice)>();

            foreach (var ci in dto.Items)
            {
                var discountedPrice = ci.UnitPrice * (1 - ci.ItemDiscountRate / 100m);
                var lineTotal = ci.Quantity * discountedPrice;
                subtotal += lineTotal;

                var cessAmount = ci.CessRate > 0
                    ? lineTotal * ci.CessRate / 100m
                    : 0;

                items.Add(new SaleItem
                {
                    ProductId = ci.ProductId,
                    ProductVariantId = ci.ProductVariantId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    ItemDiscountRate = ci.ItemDiscountRate,
                    ItemFlatDiscount = ci.ItemDiscountAmount,
                    TaxRate = ci.TaxRate,
                    IsTaxInclusive = ci.IsTaxInclusive,
                    TaxAmount = ci.TaxAmount,
                    CessRate = ci.CessRate,
                    CessAmount = cessAmount
                });

                // Deduct stock + detect price override (#294)
                if (ci.ProductVariantId.HasValue)
                {
                    var variant = await context.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.Id == ci.ProductVariantId.Value, ct)
                        .ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"Variant Id {ci.ProductVariantId} not found.");
                    if (variant.Quantity < ci.Quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock for variant {ci.ProductVariantId}. Available: {variant.Quantity}, requested: {ci.Quantity}.");
                    variant.Quantity -= ci.Quantity;

                    if (variant.Product is not null && ci.UnitPrice != variant.Product.SalePrice)
                        priceOverrides.Add((ci.ProductId, variant.Product.Name, variant.Product.SalePrice, ci.UnitPrice));
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

                    if (ci.UnitPrice != product.SalePrice)
                        priceOverrides.Add((ci.ProductId, product.Name, product.SalePrice, ci.UnitPrice));
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
                PaymentMethod = isSplit ? "Split" : dto.PaymentMethod,
                PaymentReference = dto.PaymentReference,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                DiscountAmount = discountAmount,
                DiscountReason = dto.DiscountReason,
                CashierRole = dto.CashierRole,
                IdempotencyKey = dto.IdempotencyKey,
                CustomerId = dto.CustomerId,
                Items = items
            };

            // Save split payment legs (#118)
            if (isSplit)
            {
                foreach (var leg in dto.SplitPayments!)
                {
                    sale.Payments.Add(new SalePayment
                    {
                        Method = leg.Method,
                        Amount = leg.Amount,
                        Reference = leg.Reference
                    });
                }
            }
            else
            {
                sale.Payments.Add(new SalePayment
                {
                    Method = dto.PaymentMethod,
                    Amount = sale.TotalAmount,
                    Reference = dto.PaymentReference
                });
            }

            // Credit payment creates a debtor entry (#119)
            var creditAmount = isSplit
                ? dto.SplitPayments!
                    .Where(p => string.Equals(p.Method, "Credit", StringComparison.OrdinalIgnoreCase))
                    .Sum(p => p.Amount)
                : string.Equals(dto.PaymentMethod, "Credit", StringComparison.OrdinalIgnoreCase)
                    ? sale.TotalAmount
                    : 0m;

            if (creditAmount > 0)
            {
                if (!dto.CustomerId.HasValue)
                    throw new InvalidOperationException("Credit payment requires a linked customer.");

                var customer = await context.Customers
                    .FirstOrDefaultAsync(c => c.Id == dto.CustomerId.Value, ct)
                    .ConfigureAwait(false);

                if (customer is not null)
                {
                    customer.TotalPurchaseAmount += sale.TotalAmount;
                    customer.VisitCount++;
                }

                context.Debtors.Add(new Debtor
                {
                    Name = customer?.Name ?? "Walk-in",
                    Phone = customer?.Phone ?? "",
                    TotalAmount = creditAmount,
                    PaidAmount = 0,
                    Date = regional.Now,
                    Note = $"Credit sale {invoiceNumber}"
                });
            }

            context.Sales.Add(sale);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            // Audit: sale completion (#293) + discount (#295)
            _ = auditService.LogSaleCompletedAsync(sale, dto.CashierRole, ct);
            if (discountAmount > 0)
                _ = auditService.LogDiscountAsync(sale.Id, sale.InvoiceNumber,
                    dto.DiscountType.ToString(), dto.DiscountValue, discountAmount,
                    dto.CashierRole, ct);

            // Audit: price overrides (#294)
            foreach (var po in priceOverrides)
                _ = auditService.LogPriceOverrideAsync(po.ProductId, po.ProductName,
                    po.OldPrice, po.NewPrice, dto.CashierRole, ct);

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
            .Include(v => v.Product!)
                .ThenInclude(p => p.Tax)
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

        // Use configurable invoice prefix (#313)
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var prefixBase = string.IsNullOrWhiteSpace(config?.InvoicePrefix) ? "INV" : config.InvoicePrefix.Trim();
        var prefix = $"{prefixBase}-{today:yyyyMMdd}-";

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
