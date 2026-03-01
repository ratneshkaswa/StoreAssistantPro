using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public interface ICategoryTypeService
{
    Task<List<CategoryType>> GetAllAsync();
    Task<CategoryType> CreateAsync(CategoryType categoryType);
    Task UpdateAsync(CategoryType categoryType);
    Task DeleteAsync(int id);
}
