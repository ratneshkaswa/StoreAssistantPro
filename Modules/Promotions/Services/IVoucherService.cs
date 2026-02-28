using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Promotions.Services;

public interface IVoucherService
{
    Task<List<Voucher>> GetAllAsync();
    Task<Voucher?> GetByCodeAsync(string code);
    Task<Voucher> CreateAsync(Voucher voucher);
    Task UpdateAsync(Voucher voucher);
    Task DeleteAsync(int id);
    Task<Voucher?> ValidateAndGetAsync(string code);
    Task RedeemAsync(int voucherId, decimal amount);
}
