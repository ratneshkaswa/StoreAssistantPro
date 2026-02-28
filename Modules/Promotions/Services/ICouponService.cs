using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Promotions.Services;

public interface ICouponService
{
    Task<List<Coupon>> GetAllAsync();
    Task<Coupon?> GetByCodeAsync(string code);
    Task<Coupon> CreateAsync(Coupon coupon);
    Task UpdateAsync(Coupon coupon);
    Task DeleteAsync(int id);
    Task<Coupon?> ValidateAndGetAsync(string code, decimal billAmount);
    Task IncrementUsageAsync(int couponId);
}
