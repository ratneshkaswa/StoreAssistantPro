using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Modules.Staff.Services;

public class StaffService(IDbContextFactory<AppDbContext> contextFactory) : IStaffService
{
    public async Task<List<Models.Staff>> GetAllAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Staffs.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<Models.Staff?> GetByIdAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Staffs.FindAsync(id);
    }

    public async Task<Models.Staff?> GetByCodeAsync(string code)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Staffs.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StaffCode == code);
    }

    public async Task<Models.Staff> CreateAsync(Models.Staff staff)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Staffs.Add(staff);
        await db.SaveChangesAsync();
        return staff;
    }

    public async Task UpdateAsync(Models.Staff staff)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Staffs.Update(staff);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var entity = await db.Staffs.FindAsync(id);
        if (entity is not null)
        {
            db.Staffs.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<Models.Staff>> GetActiveStaffAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Staffs.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}
