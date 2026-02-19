using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id, byte[]? rowVersion);
}
