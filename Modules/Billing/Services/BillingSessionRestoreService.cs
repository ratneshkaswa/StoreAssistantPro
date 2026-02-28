using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Deserializes a <see cref="BillingSession.SerializedBillData"/> JSON
/// payload, cross-references every line item with the current product
/// catalog, and recalculates all totals via
/// <see cref="IPricingCalculationService"/>.
/// <para>
/// <b>Product lookup strategy:</b> All referenced product IDs are batch-
/// loaded in a single query (<c>WHERE Id IN (...)</c>) to avoid N+1.
/// Products are eagerly loaded with their <see cref="TaxProfile"/> and
/// <see cref="TaxProfileItem"/>/<see cref="TaxMaster"/> chains so the
/// effective tax rate can be resolved locally.
/// </para>
/// </summary>
public class BillingSessionRestoreService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPricingCalculationService pricing,
    IPerformanceMonitor perf,
    ILogger<BillingSessionRestoreService> logger) : IBillingSessionRestoreService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<RestoredCart?> RestoreAsync(
        BillingSession session, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("BillingSessionRestoreService.RestoreAsync");

        // ── 1. Deserialize ─────────────────────────────────────────
        SerializedCart? cart;
        try
        {
            cart = JsonSerializer.Deserialize<SerializedCart>(
                session.SerializedBillData, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex,
                "Failed to deserialize billing session {SessionId}", session.SessionId);
            return null;
        }

        if (cart is null || cart.Items.Count == 0)
        {
            logger.LogWarning(
                "Session {SessionId} has no items to restore", session.SessionId);
            return null;
        }

        // ── 2. Batch-load referenced products ──────────────────────
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();

        await using var db = await contextFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        var products = await db.Products
            .AsNoTracking()
            .Include(p => p.TaxProfile!)
                .ThenInclude(tp => tp.Items)
                    .ThenInclude(tpi => tpi.TaxMaster)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct)
            .ConfigureAwait(false);

        // ── 3. Validate + recalculate each line item ───────────────
        var restoredItems = new List<RestoredCartItem>();
        var skippedItems = new List<SkippedCartItem>();

        foreach (var savedItem in cart.Items)
        {
            if (!products.TryGetValue(savedItem.ProductId, out var product))
            {
                skippedItems.Add(new SkippedCartItem
                {
                    ProductId = savedItem.ProductId,
                    ProductName = savedItem.ProductName,
                    SavedQuantity = savedItem.Quantity,
                    Reason = SkipReason.ProductDeleted
                });
                continue;
            }

            if (product.Quantity <= 0)
            {
                skippedItems.Add(new SkippedCartItem
                {
                    ProductId = savedItem.ProductId,
                    ProductName = product.Name,
                    SavedQuantity = savedItem.Quantity,
                    Reason = SkipReason.OutOfStock
                });
                continue;
            }

            // Clamp quantity to available stock
            var quantity = Math.Min(savedItem.Quantity, product.Quantity);

            // Use current catalog price and tax rate
            var currentTaxRate = GetEffectiveTaxRate(product);
            var priceChanged = product.SalePrice != savedItem.UnitPrice;

            var lineTotal = pricing.CalculateLineTotal(
                product.SalePrice, quantity, currentTaxRate, product.IsTaxInclusive);

            restoredItems.Add(new RestoredCartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantity,
                UnitPrice = product.SalePrice,
                TaxRate = currentTaxRate,
                IsTaxInclusive = product.IsTaxInclusive,
                LineTotal = lineTotal,
                PriceChanged = priceChanged
            });
        }

        if (restoredItems.Count == 0)
        {
            logger.LogWarning(
                "All items in session {SessionId} were skipped — nothing to restore",
                session.SessionId);
            return null;
        }

        // ── 4. Restore discount ────────────────────────────────────
        var discount = cart.Discount is not null
            ? new BillDiscount
            {
                Type = cart.Discount.Type,
                Value = cart.Discount.Value,
                Reason = cart.Discount.Reason
            }
            : BillDiscount.None;

        // ── 5. Compute totals ──────────────────────────────────────
        var subtotal = restoredItems.Sum(i => i.LineTotal.Subtotal);
        var totalTax = restoredItems.Sum(i => i.LineTotal.TaxAmount);
        var grandTotal = restoredItems.Sum(i => i.LineTotal.FinalAmount);

        logger.LogInformation(
            "Restored session {SessionId}: {Restored} items, {Skipped} skipped, total {GrandTotal:C}",
            session.SessionId, restoredItems.Count, skippedItems.Count, grandTotal);

        return new RestoredCart
        {
            SessionId = cart.SessionId,
            Items = restoredItems,
            Discount = discount,
            SkippedItems = skippedItems,
            Subtotal = subtotal,
            TotalTax = totalTax,
            GrandTotal = grandTotal
        };
    }

    /// <summary>
    /// Sums the tax-component rates from the product's tax profile.
    /// Returns 0 when the product has no profile or the profile has no items.
    /// </summary>
    private static decimal GetEffectiveTaxRate(Product product)
    {
        if (product.TaxProfile?.Items is not { Count: > 0 } items)
            return 0m;

        return items
            .Where(i => i.TaxMaster is { IsActive: true })
            .Sum(i => i.TaxMaster!.TaxRate);
    }
}
