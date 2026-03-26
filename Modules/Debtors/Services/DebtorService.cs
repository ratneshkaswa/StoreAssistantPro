using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Debtors.Services;

public class DebtorService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IDebtorService
{
    public async Task<IReadOnlyList<Debtor>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DebtorService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Debtors
            .AsNoTracking()
            .OrderByDescending(d => d.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Debtor?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DebtorService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Debtors
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(DebtorDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        using var _ = perf.BeginScope("DebtorService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new Debtor
        {
            Name = dto.Name.Trim(),
            Phone = dto.Phone?.Trim() ?? string.Empty,
            TotalAmount = dto.TotalAmount,
            PaidAmount = dto.PaidAmount,
            Date = dto.Date,
            Note = dto.Note?.Trim() ?? string.Empty
        };

        context.Debtors.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, DebtorDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("DebtorService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Debtors.FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Debtor with Id {id} not found.");

        entity.Name = dto.Name.Trim();
        entity.Phone = dto.Phone?.Trim() ?? string.Empty;
        entity.TotalAmount = dto.TotalAmount;
        entity.PaidAmount = dto.PaidAmount;
        entity.Date = dto.Date;
        entity.Note = dto.Note?.Trim() ?? string.Empty;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DebtorService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Debtors.FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Debtor with Id {id} not found.");

        context.Debtors.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordPaymentAsync(int id, decimal amount, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DebtorService.RecordPaymentAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Debtors.FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Debtor with Id {id} not found.");

        entity.PaidAmount += amount;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<DebtorStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DebtorService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var all = await context.Debtors.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        return new DebtorStats(
            all.Count,
            all.Count(d => d.TotalAmount > d.PaidAmount),
            all.Sum(d => d.TotalAmount - d.PaidAmount),
            all.Sum(d => d.PaidAmount));
    }
}
