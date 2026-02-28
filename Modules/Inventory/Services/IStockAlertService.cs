using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inventory.Services;

public interface IStockAlertService
{
    Task<List<StockAlert>> GetAllAsync();
    Task<StockAlert?> GetByProductIdAsync(int productId);
    Task<StockAlert> CreateOrUpdateAsync(StockAlert alert);
    Task DeleteAsync(int id);
    Task<List<Product>> GetLowStockProductsAsync();
    Task<List<Product>> GetOverStockProductsAsync();
}
