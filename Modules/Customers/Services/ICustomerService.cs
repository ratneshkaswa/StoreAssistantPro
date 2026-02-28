using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Customers.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<List<Customer>> SearchAsync(string query);
    Task<Customer> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task AddLoyaltyPointsAsync(int customerId, int points);
    Task UpdatePurchaseStatsAsync(int customerId, decimal amount);
}
