using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Billing.Events;

namespace StoreAssistantPro.Modules.Billing.Services;

public class SaleReturnService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILoginService loginService,
    IAuditService auditService,
    ICashRegisterService cashRegisterService,
    Func<IBillingService> _billingServiceFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional,
    IEventBus eventBus) : ISaleReturnService
{
    public async Task<SaleReturn> ProcessReturnAsync(SaleReturnDto dto, CancellationToken ct = default)
    {
        var saleReturn = await ProcessReturnCoreAsync(dto, ct).ConfigureAwait(false);
        await PublishSalesDataChangedAsync("SaleReturnProcessed").ConfigureAwait(false);
        return saleReturn;
    }

    private async Task<SaleReturn> ProcessReturnCoreAsync(SaleReturnDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Manager/admin PIN approval required (#148)
        if (string.IsNullOrWhiteSpace(dto.ApproverPin))
            throw new InvalidOperationException("Manager PIN is required to approve returns.");

        var pinValid = await loginService.ValidateMasterPinAsync(dto.ApproverPin, ct);
        if (!pinValid)
            throw new InvalidOperationException("Invalid approval PIN.");

        // Day close lockdown (#246): block returns after register is closed for the day
        if (await cashRegisterService.IsDayClosedAsync(regional.Now, ct).ConfigureAwait(false))
            throw new InvalidOperationException("Business day is closed. No more returns allowed today.");

        using var scope = perf.BeginScope("SaleReturnService.ProcessReturnAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            var saleItem = await context.SaleItems
                .Include(si => si.Product)
                .FirstOrDefaultAsync(si => si.Id == dto.SaleItemId && si.SaleId == dto.SaleId, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"SaleItem {dto.SaleItemId} not found in Sale {dto.SaleId}.");

            // Check not returning more than originally sold
            var alreadyReturned = await context.SaleReturns
                .Where(r => r.SaleItemId == dto.SaleItemId)
                .SumAsync(r => r.Quantity, ct)
                .ConfigureAwait(false);

            var maxReturnable = saleItem.Quantity - alreadyReturned;
            if (dto.QuantityReturned > maxReturnable)
                throw new InvalidOperationException(
                    $"Cannot return {dto.QuantityReturned}. Max returnable: {maxReturnable}.");

            // Calculate refund
            var refundPerUnit = saleItem.DiscountedUnitPrice;
            var refundAmount = dto.QuantityReturned * refundPerUnit;

            // Generate return number and credit note number
            var returnNumber = await GenerateReturnNumberAsync(context, ct);
            var creditNoteNumber = await GenerateCreditNoteNumberAsync(context, ct);

            var saleReturn = new SaleReturn
            {
                ReturnNumber = returnNumber,
                CreditNoteNumber = creditNoteNumber,
                SaleId = dto.SaleId,
                SaleItemId = dto.SaleItemId,
                Quantity = dto.QuantityReturned,
                RefundAmount = refundAmount,
                Reason = dto.Reason,
                ReturnDate = regional.Now,
                ApprovedByRole = "Master"
            };

            context.SaleReturns.Add(saleReturn);

            // Restore stock to the correct entity (variant or product)
            if (saleItem.ProductVariantId.HasValue)
            {
                var variant = await context.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == saleItem.ProductVariantId.Value, ct)
                    .ConfigureAwait(false)
                    ?? throw new InvalidOperationException(
                        $"ProductVariant Id {saleItem.ProductVariantId} no longer exists. Cannot restore stock for return.");

                variant.Quantity += dto.QuantityReturned;
            }
            else
            {
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.Id == saleItem.ProductId, ct)
                    .ConfigureAwait(false)
                    ?? throw new InvalidOperationException(
                        $"Product Id {saleItem.ProductId} no longer exists. Cannot restore stock for return.");

                product.Quantity += dto.QuantityReturned;
            }
            saleReturn.StockRestored = true;

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            // Audit: return processed (#296)
            _ = auditService.LogReturnAsync(saleReturn, "Master", ct);

            return saleReturn;
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<IReadOnlyList<SaleReturn>> GetReturnsBySaleAsync(int saleId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SaleReturnService.GetReturnsBySaleAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.SaleReturns
            .AsNoTracking()
            .Include(r => r.SaleItem).ThenInclude(si => si!.Product)
            .Where(r => r.SaleId == saleId)
            .OrderByDescending(r => r.ReturnDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SaleReturn>> GetRecentReturnsAsync(int count = 100, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SaleReturnService.GetRecentReturnsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.SaleReturns
            .AsNoTracking()
            .Include(r => r.Sale)
            .Include(r => r.SaleItem).ThenInclude(si => si!.Product)
            .OrderByDescending(r => r.ReturnDate)
            .Take(count)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<ExchangeResult> ExchangeAsync(ExchangeDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("SaleReturnService.ExchangeAsync");

        // 1. Process the return (validates PIN, restores stock, creates credit note)
        var saleReturn = await ProcessReturnCoreAsync(dto.Return, ct).ConfigureAwait(false);

        // 2. Complete the new sale (stock deduction, invoice creation)
        // The calling VM can adjust CashTendered to account for the credit
        var billingService = GetBillingService();
        var newSale = await billingService.CompleteSaleAsync(dto.NewSale, ct).ConfigureAwait(false);
        await PublishSalesDataChangedAsync("SaleExchangeCompleted").ConfigureAwait(false);

        return new ExchangeResult(saleReturn, newSale, saleReturn.RefundAmount);
    }

    private IBillingService GetBillingService()
    {
        // Resolved lazily to avoid circular DI
        return _billingServiceFactory();
    }

    private async Task<string> GenerateReturnNumberAsync(AppDbContext context, CancellationToken ct)
    {
        var today = regional.Now.Date;
        var prefix = $"RET-{today:yyyyMMdd}-";

        var last = await context.SaleReturns
            .Where(r => r.ReturnNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReturnNumber)
            .Select(r => r.ReturnNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (last is not null)
        {
            var seqPart = last[prefix.Length..];
            if (int.TryParse(seqPart, out var seq))
                next = seq + 1;
        }
        return $"{prefix}{next:D4}";
    }

    private async Task<string> GenerateCreditNoteNumberAsync(AppDbContext context, CancellationToken ct)
    {
        var today = regional.Now.Date;
        var prefix = $"CN-{today:yyyyMMdd}-";

        var last = await context.SaleReturns
            .Where(r => r.CreditNoteNumber.StartsWith(prefix))
            .OrderByDescending(r => r.CreditNoteNumber)
            .Select(r => r.CreditNoteNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (last is not null)
        {
            var seqPart = last[prefix.Length..];
            if (int.TryParse(seqPart, out var seq))
                next = seq + 1;
        }
        return $"{prefix}{next:D4}";
    }

    // ── No-bill return (#153) ────────────────────────────────────────

    public async Task<SaleReturn> NoBillReturnAsync(NoBillReturnDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("SaleReturnService.NoBillReturnAsync");

        if (string.IsNullOrWhiteSpace(dto.ApproverPin))
            throw new InvalidOperationException("Admin PIN is required for no-bill returns.");

        var pinValid = await loginService.ValidateMasterPinAsync(dto.ApproverPin, ct);
        if (!pinValid)
            throw new InvalidOperationException("Invalid admin PIN.");

        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Restore stock
        if (dto.ProductVariantId.HasValue)
        {
            var variant = await context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == dto.ProductVariantId.Value, ct)
                .ConfigureAwait(false);
            if (variant is not null)
                variant.Quantity += dto.QuantityReturned;
        }
        else
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId, ct)
                .ConfigureAwait(false);
            if (product is not null)
                product.Quantity += dto.QuantityReturned;
        }

        var returnNumber = await GenerateReturnNumberAsync(context, ct);
        var creditNoteNumber = await GenerateCreditNoteNumberAsync(context, ct);

        var saleReturn = new SaleReturn
        {
            ReturnNumber = returnNumber,
            SaleId = 0,
            SaleItemId = 0,
            Quantity = dto.QuantityReturned,
            RefundAmount = dto.RefundAmount,
            CreditNoteNumber = creditNoteNumber,
            Reason = dto.Reason,
            ProcessedByRole = "Admin",
            ApprovedByRole = "Admin",
            ReturnDate = regional.Now,
            StockRestored = true,
            IsNoBillReturn = true
        };

        context.SaleReturns.Add(saleReturn);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditService.LogAsync("NoBillReturn", "SaleReturn", saleReturn.Id.ToString(),
            null, null, null, $"No-bill return: Product {dto.ProductId}, Qty {dto.QuantityReturned}, Refund {dto.RefundAmount}", ct);
        await PublishSalesDataChangedAsync("NoBillReturnProcessed").ConfigureAwait(false);

        return saleReturn;
    }

    private Task PublishSalesDataChangedAsync(string reason)
        => eventBus.PublishAsync(new SalesDataChangedEvent(reason, DateTime.UtcNow));
}
