using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.FinancialYears.Services;

public class FinancialYearService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional) : IFinancialYearService
{
    public async Task<List<FinancialYear>> GetAllAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.FinancialYears.AsNoTracking()
            .OrderByDescending(f => f.StartDate)
            .ToListAsync();
    }

    public async Task<FinancialYear?> GetCurrentAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.FinancialYears.AsNoTracking()
            .FirstOrDefaultAsync(f => f.IsCurrent);
    }

    public async Task<FinancialYear> CreateAsync(FinancialYear fy)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.FinancialYears.Add(fy);
        await db.SaveChangesAsync();
        return fy;
    }

    public async Task SetCurrentAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var all = await db.FinancialYears.ToListAsync();
        foreach (var fy in all)
            fy.IsCurrent = fy.Id == id;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Ensures a FinancialYear record exists for the current Indian FY (Apr–Mar).
    /// Creates one if missing and marks it current.
    /// </summary>
    public async Task<FinancialYear> EnsureCurrentFYAsync()
    {
        var now = regional.Now;
        // Indian FY: Apr 1 to Mar 31
        var fyStartYear = now.Month >= 4 ? now.Year : now.Year - 1;
        var fyName = $"{fyStartYear}–{(fyStartYear + 1) % 100:D2}";

        await using var db = await contextFactory.CreateDbContextAsync();
        var existing = await db.FinancialYears.FirstOrDefaultAsync(f => f.Name == fyName);
        if (existing is not null)
        {
            if (!existing.IsCurrent)
            {
                foreach (var fy in await db.FinancialYears.Where(f => f.IsCurrent).ToListAsync())
                    fy.IsCurrent = false;
                existing.IsCurrent = true;
                await db.SaveChangesAsync();
            }
            return existing;
        }

        // Deactivate all others
        foreach (var fy in await db.FinancialYears.Where(f => f.IsCurrent).ToListAsync())
            fy.IsCurrent = false;

        var newFy = new FinancialYear
        {
            Name = fyName,
            StartDate = new DateTime(fyStartYear, 4, 1),
            EndDate = new DateTime(fyStartYear + 1, 3, 31),
            IsCurrent = true
        };
        db.FinancialYears.Add(newFy);
        await db.SaveChangesAsync();
        return newFy;
    }
}
