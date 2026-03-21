using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Modules.Billing.Services;

public class SaleReturnService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILoginService loginService,
    IAuditService auditService,
    ICashRegisterService cashRegisterService,
    Func<IBillingService> _billingServiceFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : ISaleReturnService
{
    public async Task<SaleReturn> ProcessReturnAsync(SaleReturnDto dto, CancellationToken ct = default)
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
        var saleReturn = await ProcessReturnAsync(dto.Return, ct);

        // 2. Complete the new sale (stock deduction, invoice creation)
        // The calling VM can adjust CashTendered to account for the credit
        var billingService = GetBillingService();
        var newSale = await billingService.CompleteSaleAsync(dto.NewSale, ct);

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
}
