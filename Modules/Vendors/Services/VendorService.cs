using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Vendors.Services;

public class VendorService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IVendorService
{
    public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Vendors
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Vendor>> GetActiveAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorService.GetActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Vendors
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Vendor>> SearchAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        using var _ = perf.BeginScope("VendorService.SearchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = query.Trim();
        return await context.Vendors
            .AsNoTracking()
            .Where(v => v.Name.Contains(trimmed)
                     || (v.ContactPerson != null && v.ContactPerson.Contains(trimmed))
                     || (v.GSTIN != null && v.GSTIN.Contains(trimmed))
                     || (v.City != null && v.City.Contains(trimmed)))
            .OrderBy(v => v.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
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
    }

    private static void ValidateDto(VendorDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name, nameof(dto.Name));

        if (!string.IsNullOrWhiteSpace(dto.GSTIN) && !GstinPattern.IsMatch(dto.GSTIN.Trim()))
            throw new ArgumentException("Invalid GSTIN format (expected 15-char alphanumeric).", nameof(dto.GSTIN));

        if (!string.IsNullOrWhiteSpace(dto.PAN) && !PanPattern.IsMatch(dto.PAN.Trim()))
            throw new ArgumentException("Invalid PAN format (expected ABCDE1234F).", nameof(dto.PAN));
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Regex GstinPattern = new(@"^\d{2}[A-Z]{5}\d{4}[A-Z]\d[Z][A-Z\d]$", RegexOptions.Compiled);
    private static readonly Regex PanPattern = new(@"^[A-Z]{5}\d{4}[A-Z]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
}
