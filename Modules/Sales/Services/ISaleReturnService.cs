using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Sales.Services;

public interface ISaleReturnService
{
    Task<List<SaleReturn>> GetAllAsync();
    Task<List<SaleReturn>> GetBySaleIdAsync(int saleId);
    Task<SaleReturn> ProcessReturnAsync(SaleReturn saleReturn);
    Task<string> GenerateReturnNumberAsync();
}
