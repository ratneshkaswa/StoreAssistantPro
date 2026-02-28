using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Services;

public class TaxService(IDbContextFactory<AppDbContext> contextFactory) : ITaxService
{
    public async Task<IReadOnlyList<TaxProfile>> GetAllProfilesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.TaxProfiles
            .Include(p => p.Items).ThenInclude(i => i.TaxMaster)
            .OrderBy(p => p.ProfileName)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<TaxProfile> GetProfileWithItemsAsync(int profileId)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.TaxProfiles
            .Include(p => p.Items).ThenInclude(i => i.TaxMaster)
            .FirstAsync(p => p.Id == profileId)
            .ConfigureAwait(false);
    }

    public async Task AddProfileAsync(TaxProfile profile)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.TaxProfiles.Add(profile);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateProfileAsync(TaxProfile profile)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.TaxProfiles.Update(profile);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SetActiveAsync(int profileId, bool isActive)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var profile = await context.TaxProfiles.FindAsync(profileId).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Tax profile not found.");
        profile.IsActive = isActive;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SetDefaultAsync(int profileId)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // Clear existing default
        var currentDefaults = await context.TaxProfiles
            .Where(p => p.IsDefault)
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var p in currentDefaults)
            p.IsDefault = false;

        // Set new default
        var profile = await context.TaxProfiles.FindAsync(profileId).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Tax profile not found.");
        profile.IsDefault = true;

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> IsProfileUsedByProductsAsync(int profileId)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Products
            .AnyAsync(p => p.TaxProfileId == profileId)
            .ConfigureAwait(false);
    }
}
