using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Promotions.Services;

public class CouponService(IDbContextFactory<AppDbContext> contextFactory) : ICouponService
{
    public async Task<List<Coupon>> GetAllAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Coupons.AsNoTracking().OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code);
    }

    public async Task<Coupon> CreateAsync(Coupon coupon)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();
        return coupon;
    }

    public async Task UpdateAsync(Coupon coupon)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Coupons.Update(coupon);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var entity = await db.Coupons.FindAsync(id);
        if (entity is not null)
        {
            db.Coupons.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<Coupon?> ValidateAndGetAsync(string code, decimal billAmount)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == code);
        if (coupon is null || !coupon.IsValid || billAmount < coupon.MinBillAmount)
            return null;
        return coupon;
    }

    public async Task IncrementUsageAsync(int couponId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var coupon = await db.Coupons.FindAsync(couponId);
        if (coupon is not null)
        {
            coupon.UsedCount++;
            await db.SaveChangesAsync();
        }
    }
}
