using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Persists held bills to database (#336-#346).
/// Uses short-lived DbContext per operation.
/// </summary>
public class HeldBillService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional) : IHeldBillService
{
    public async Task<HeldBill> HoldAsync(HoldBillDto dto, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Enforce max held bills per user (#347)
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (config is not null && config.MaxHeldBillsPerUser > 0)
        {
            var activeCount = await context.HeldBills
                .CountAsync(h => h.IsActive && h.CashierRole == dto.CashierRole, ct)
                .ConfigureAwait(false);
            if (activeCount >= config.MaxHeldBillsPerUser)
                throw new InvalidOperationException(
                    $"Maximum held bills limit ({config.MaxHeldBillsPerUser}) reached. Recall or discard existing held bills first.");
        }

        var entity = new HeldBill
        {
            Label = dto.Label,
            CustomerTag = dto.CustomerTag,
            Notes = dto.Notes,
            CashierRole = dto.CashierRole,
            HeldAt = regional.Now,
            Total = dto.Total,
            ItemCount = dto.Items.Count,
            IsActive = true,
            Items = dto.Items.Select(i => new HeldBillItem
            {
                ProductId = i.ProductId,
                ProductVariantId = i.ProductVariantId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TaxRate = i.TaxRate,
                IsTaxInclusive = i.IsTaxInclusive,
                ItemDiscountRate = i.ItemDiscountRate,
                ItemDiscountAmount = i.ItemDiscountAmount,
                CessRate = i.CessRate
            }).ToList()
        };

        context.HeldBills.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }

    public async Task<HeldBill?> RecallAsync(int heldBillId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var bill = await context.HeldBills
            .Include(h => h.Items)
            .FirstOrDefaultAsync(h => h.Id == heldBillId && h.IsActive, ct)
            .ConfigureAwait(false);

        if (bill is null) return null;

        bill.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return bill;
    }

    public async Task<IReadOnlyList<HeldBill>> GetActiveAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.HeldBills
            .AsNoTracking()
            .Include(h => h.Items)
            .Where(h => h.IsActive)
            .OrderByDescending(h => h.HeldAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.HeldBills
            .CountAsync(h => h.IsActive, ct)
            .ConfigureAwait(false);
    }

    public async Task DiscardAsync(int heldBillId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var bill = await context.HeldBills
            .FirstOrDefaultAsync(h => h.Id == heldBillId && h.IsActive, ct)
            .ConfigureAwait(false);

        if (bill is not null)
        {
            bill.IsActive = false;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<int> CleanupStaleAsync(DateTime cutoff, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var stale = await context.HeldBills
            .Where(h => h.IsActive && h.HeldAt < cutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var bill in stale)
            bill.IsActive = false;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return stale.Count;
    }

    public async Task AutoCleanupStaleAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (config is null || config.HeldBillTimeoutMinutes <= 0) return;

        var cutoff = DateTime.UtcNow.AddMinutes(-config.HeldBillTimeoutMinutes);
        var stale = await context.HeldBills
            .Where(h => h.IsActive && h.HeldAt < cutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var bill in stale)
            bill.IsActive = false;

        if (stale.Count > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HeldBill>> GetHistoryAsync(int count = 50, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.HeldBills
            .AsNoTracking()
            .OrderByDescending(h => h.HeldAt)
            .Take(count)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
