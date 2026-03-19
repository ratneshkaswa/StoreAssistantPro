using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Customers.Services;

public class CustomerService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
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

    public async Task<PagedResult<Customer>> GetPagedAsync(PagedQuery query, string? search = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(c => c.Name.Contains(term) || (c.Phone != null && c.Phone.Contains(term)));
        }

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var items = await q
            .OrderBy(c => c.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<Customer>(items, totalCount, query.Page, query.PageSize);
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

        var gstinError = GstinValidator.Validate(dto.GSTIN);
        if (gstinError is not null)
            throw new ArgumentException(gstinError, nameof(dto.GSTIN));

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
            CreatedDate = regional.Now
        });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, CustomerDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var gstinError = GstinValidator.Validate(dto.GSTIN);
        if (gstinError is not null)
            throw new ArgumentException(gstinError, nameof(dto.GSTIN));

        using var _ = perf.BeginScope("CustomerService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer Id {id} not found.");

        // Duplicate phone detection on update
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var phoneTaken = await context.Customers
                .AnyAsync(c => c.Phone == dto.Phone.Trim() && c.Id != id, ct)
                .ConfigureAwait(false);
            if (phoneTaken)
                throw new InvalidOperationException($"Another customer already has phone {dto.Phone}.");
        }

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

    public async Task<int> ImportBulkAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.ImportBulkAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existing = await context.Customers
            .AsNoTracking()
            .Select(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingNames = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var now = regional.Now;
        var count = 0;

        foreach (var row in rows)
        {
            var name = (row.GetValueOrDefault("Name") ?? row.GetValueOrDefault("Customer") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || !existingNames.Add(name))
                continue;

            context.Customers.Add(new Customer
            {
                Name = name,
                Phone = NullIfEmpty(row.GetValueOrDefault("Phone")),
                Email = NullIfEmpty(row.GetValueOrDefault("Email")),
                Address = NullIfEmpty(row.GetValueOrDefault("Address")),
                GSTIN = NullIfEmpty(row.GetValueOrDefault("GSTIN")),
                Notes = NullIfEmpty(row.GetValueOrDefault("Notes")),
                IsActive = true,
                CreatedDate = now
            });
            count++;
        }

        if (count > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return count;
    }

    public async Task<IReadOnlyList<CustomerPurchaseSummary>> GetPurchaseHistoryAsync(int customerId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetPurchaseHistoryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Sales
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new CustomerPurchaseSummary(
                s.Id,
                s.InvoiceNumber,
                s.SaleDate,
                s.TotalAmount,
                s.PaymentMethod,
                s.Items.Count))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
