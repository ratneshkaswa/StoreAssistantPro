using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Vendors.Services;

public class VendorService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional) : IVendorService
{
    public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Vendors
            .OrderBy(v => v.Name)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Vendor>> GetActiveAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Vendors
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Vendor?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Vendors.FindAsync([id], ct).ConfigureAwait(false);
    }

    public async Task AddAsync(Vendor vendor, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        vendor.CreatedDate = regional.Now;
        context.Vendors.Add(vendor);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Vendor vendor, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        vendor.ModifiedDate = regional.Now;
        context.Vendors.Update(vendor);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(int id, byte[]? rowVersion, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Vendors.FindAsync([id], ct).ConfigureAwait(false);
        if (entity is null) return;
        if (rowVersion is not null) context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
        entity.IsActive = !entity.IsActive;
        entity.ModifiedDate = regional.Now;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Vendors.FindAsync([id], ct).ConfigureAwait(false);
        if (entity is null) return;
        if (rowVersion is not null) context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
        context.Vendors.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Vendors.CountAsync(ct).ConfigureAwait(false);
    }
}
