using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Promotions.Services;

public class VoucherService(IDbContextFactory<AppDbContext> contextFactory) : IVoucherService
{
    public async Task<List<Voucher>> GetAllAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Vouchers.AsNoTracking()
            .Include(v => v.Customer)
            .OrderByDescending(v => v.IssuedDate)
            .ToListAsync();
    }

    public async Task<Voucher?> GetByCodeAsync(string code)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Vouchers.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Code == code);
    }

    public async Task<Voucher> CreateAsync(Voucher voucher)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Vouchers.Add(voucher);
        await db.SaveChangesAsync();
        return voucher;
    }

    public async Task UpdateAsync(Voucher voucher)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Vouchers.Update(voucher);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var entity = await db.Vouchers.FindAsync(id);
        if (entity is not null)
        {
            db.Vouchers.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<Voucher?> ValidateAndGetAsync(string code)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
        return voucher is { IsValid: true } ? voucher : null;
    }

    public async Task RedeemAsync(int voucherId, decimal amount)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var voucher = await db.Vouchers.FindAsync(voucherId);
        if (voucher is not null && voucher.Balance >= amount)
        {
            voucher.Balance -= amount;
            await db.SaveChangesAsync();
        }
    }
}
