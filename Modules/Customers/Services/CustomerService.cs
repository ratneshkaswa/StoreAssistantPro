using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Customers.Services;

public class CustomerService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : ICustomerService
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        using var _ = perf.BeginScope("CustomerService.SearchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var term = query.Trim();
        return await context.Customers
            .AsNoTracking()
            .Where(c => c.Name.Contains(term) || (c.Phone != null && c.Phone.Contains(term)))
            .OrderBy(c => c.Name)
            .Take(50)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        using var _ = perf.BeginScope("CustomerService.GetByPhoneAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Phone == phone.Trim(), ct)
            .ConfigureAwait(false);
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(CustomerDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        using var _ = perf.BeginScope("CustomerService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Duplicate phone detection
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var existing = await context.Customers
                .AnyAsync(c => c.Phone == dto.Phone.Trim(), ct)
                .ConfigureAwait(false);
            if (existing)
                throw new InvalidOperationException($"Customer with phone {dto.Phone} already exists.");
        }

        context.Customers.Add(new Customer
        {
            Name = dto.Name.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            GSTIN = dto.GSTIN?.Trim(),
            Notes = dto.Notes?.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, CustomerDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        using var _ = perf.BeginScope("CustomerService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer Id {id} not found.");

        entity.Name = dto.Name.Trim();
        entity.Phone = dto.Phone?.Trim();
        entity.Email = dto.Email?.Trim();
        entity.Address = dto.Address?.Trim();
        entity.GSTIN = dto.GSTIN?.Trim();
        entity.Notes = dto.Notes?.Trim();

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
