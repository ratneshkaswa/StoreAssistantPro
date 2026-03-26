using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Payments.Services;

public class PaymentService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IPaymentService
{
    public async Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PaymentService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Payments
            .Include(p => p.Customer)
            .AsNoTracking()
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Payment>> GetByCustomerAsync(int customerId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PaymentService.GetByCustomerAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(PaymentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("PaymentService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new Payment
        {
            CustomerId = dto.CustomerId,
            PaymentDate = dto.PaymentDate,
            Amount = dto.Amount,
            Note = dto.Note?.Trim() ?? string.Empty
        };

        context.Payments.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, PaymentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("PaymentService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Payments.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Payment with Id {id} not found.");

        entity.CustomerId = dto.CustomerId;
        entity.PaymentDate = dto.PaymentDate;
        entity.Amount = dto.Amount;
        entity.Note = dto.Note?.Trim() ?? string.Empty;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PaymentService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Payments.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Payment with Id {id} not found.");

        context.Payments.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<PaymentStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PaymentService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var all = await context.Payments.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        return new PaymentStats(
            all.Count,
            all.Sum(p => p.Amount));
    }

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("PaymentService.GetCustomersAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
