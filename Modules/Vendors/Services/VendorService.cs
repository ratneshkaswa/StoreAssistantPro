using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Vendors.Services;

public class VendorService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    IReferenceDataCache referenceDataCache) : IVendorService
{
    private static readonly TimeSpan ReferenceDataTtl = TimeSpan.FromMinutes(5);
    private const string ActiveVendorsCacheKey = "Vendors.Active";

    public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var productCounts = await context.Products
            .Where(p => p.VendorId.HasValue)
            .GroupBy(p => p.VendorId!.Value)
            .Select(g => new { VendorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.VendorId, g => g.Count, ct)
            .ConfigureAwait(false);

        var vendors = await context.Vendors
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var vendor in vendors)
            vendor.ProductCount = productCounts.GetValueOrDefault(vendor.Id);

        return vendors;
    }

    public async Task<PagedResult<Vendor>> GetPagedAsync(PagedQuery query, string? search = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Vendors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(v => v.Name.Contains(term)
                         || (v.ContactPerson != null && v.ContactPerson.Contains(term))
                         || (v.GSTIN != null && v.GSTIN.Contains(term))
                         || (v.City != null && v.City.Contains(term)));
        }

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var vendors = await q
            .OrderBy(v => v.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (vendors.Count > 0)
        {
            var vendorIds = vendors.Select(v => v.Id).ToList();
            var productCounts = await context.Products
                .Where(p => p.VendorId.HasValue && vendorIds.Contains(p.VendorId.Value))
                .GroupBy(p => p.VendorId!.Value)
                .Select(g => new { VendorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.VendorId, g => g.Count, ct)
                .ConfigureAwait(false);

            foreach (var vendor in vendors)
                vendor.ProductCount = productCounts.GetValueOrDefault(vendor.Id);
        }

        return new PagedResult<Vendor>(vendors, totalCount, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<Vendor>> GetActiveAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorService.GetActiveAsync");
        return await referenceDataCache.GetOrCreateAsync<Vendor>(
            ActiveVendorsCacheKey,
            async innerCt =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(innerCt).ConfigureAwait(false);
                return await context.Vendors
                    .AsNoTracking()
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Name)
                    .ToListAsync(innerCt)
                    .ConfigureAwait(false);
            },
            ReferenceDataTtl,
            ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Vendor>> SearchAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        using var _ = perf.BeginScope("VendorService.SearchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = query.Trim();
        var vendors = await context.Vendors
            .AsNoTracking()
            .Where(v => v.Name.Contains(trimmed)
                     || (v.ContactPerson != null && v.ContactPerson.Contains(trimmed))
                     || (v.GSTIN != null && v.GSTIN.Contains(trimmed))
                     || (v.City != null && v.City.Contains(trimmed)))
            .OrderBy(v => v.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var vendorIds = vendors.Select(v => v.Id).ToList();
        var productCounts = await context.Products
            .Where(p => p.VendorId.HasValue && vendorIds.Contains(p.VendorId.Value))
            .GroupBy(p => p.VendorId!.Value)
            .Select(g => new { VendorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.VendorId, g => g.Count, ct)
            .ConfigureAwait(false);

        foreach (var vendor in vendors)
            vendor.ProductCount = productCounts.GetValueOrDefault(vendor.Id);

        return vendors;
    }

    public async Task<Vendor?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(VendorDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("VendorService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmedName = dto.Name.Trim();
        if (await context.Vendors.AnyAsync(v => v.Name == trimmedName, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Vendor '{trimmedName}' already exists.");

        var now = regional.Now;

        var entity = new Vendor
        {
            Name = trimmedName,
            ContactPerson = dto.ContactPerson?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            AddressLine2 = dto.AddressLine2?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            PinCode = dto.PinCode?.Trim(),
            GSTIN = NullIfEmpty(dto.GSTIN),
            PAN = NullIfEmpty(dto.PAN)?.ToUpperInvariant(),
            TransportPreference = dto.TransportPreference?.Trim(),
            PaymentTerms = dto.PaymentTerms?.Trim(),
            CreditLimit = dto.CreditLimit,
            OpeningBalance = dto.OpeningBalance,
            Notes = dto.Notes?.Trim(),
            IsActive = true,
            CreatedDate = now
        };

        context.Vendors.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveVendorsCacheKey);
    }

    public async Task UpdateAsync(int id, VendorDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("VendorService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Vendor with Id {id} not found.");

        var trimmedName = dto.Name.Trim();
        if (await context.Vendors.AnyAsync(v => v.Name == trimmedName && v.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Vendor '{trimmedName}' already exists.");

        entity.Name = trimmedName;
        entity.ContactPerson = dto.ContactPerson?.Trim();
        entity.Phone = dto.Phone?.Trim();
        entity.Email = dto.Email?.Trim();
        entity.Address = dto.Address?.Trim();
        entity.AddressLine2 = dto.AddressLine2?.Trim();
        entity.City = dto.City?.Trim();
        entity.State = dto.State?.Trim();
        entity.PinCode = dto.PinCode?.Trim();
        entity.GSTIN = NullIfEmpty(dto.GSTIN);
        entity.PAN = NullIfEmpty(dto.PAN)?.ToUpperInvariant();
        entity.TransportPreference = dto.TransportPreference?.Trim();
        entity.PaymentTerms = dto.PaymentTerms?.Trim();
        entity.CreditLimit = dto.CreditLimit;
        entity.OpeningBalance = dto.OpeningBalance;
        entity.Notes = dto.Notes?.Trim();
        entity.ModifiedDate = regional.Now;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveVendorsCacheKey);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Vendor with Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        entity.ModifiedDate = regional.Now;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveVendorsCacheKey);
    }

    public async Task<int> ImportBulkAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorService.ImportBulkAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existing = await context.Vendors
            .AsNoTracking()
            .Select(v => v.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingNames = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var now = regional.Now;
        var count = 0;

        foreach (var row in rows)
        {
            var name = (row.GetValueOrDefault("Name") ?? row.GetValueOrDefault("Vendor") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || !existingNames.Add(name))
                continue;

            context.Vendors.Add(new Vendor
            {
                Name = name,
                ContactPerson = NullIfEmpty(row.GetValueOrDefault("ContactPerson")),
                Phone = NullIfEmpty(row.GetValueOrDefault("Phone")),
                Email = NullIfEmpty(row.GetValueOrDefault("Email")),
                Address = NullIfEmpty(row.GetValueOrDefault("Address")),
                AddressLine2 = NullIfEmpty(row.GetValueOrDefault("AddressLine2")),
                City = NullIfEmpty(row.GetValueOrDefault("City")),
                State = NullIfEmpty(row.GetValueOrDefault("State")),
                PinCode = NullIfEmpty(row.GetValueOrDefault("PinCode")),
                GSTIN = NullIfEmpty(row.GetValueOrDefault("GSTIN")),
                PAN = NullIfEmpty(row.GetValueOrDefault("PAN"))?.ToUpperInvariant(),
                TransportPreference = NullIfEmpty(row.GetValueOrDefault("TransportPreference")),
                IsActive = true,
                CreatedDate = now
            });
            count++;
        }

        if (count > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return count;
    }

    private static void ValidateDto(VendorDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name, nameof(dto.Name));

        var gstinError = GstinValidator.Validate(dto.GSTIN);
        if (gstinError is not null)
            throw new ArgumentException(gstinError, nameof(dto.GSTIN));

        var panError = GstinValidator.ValidatePan(dto.PAN);
        if (panError is not null)
            throw new ArgumentException(panError, nameof(dto.PAN));
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
