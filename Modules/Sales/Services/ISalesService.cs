using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Sales.Services;

public interface ISalesService
{
    Task<IEnumerable<Sale>> GetAllAsync();
    Task<Sale?> GetByIdAsync(int id);
    Task CreateSaleAsync(Sale sale);
    Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to);
}
