using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
}
