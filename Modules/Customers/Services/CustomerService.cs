using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Customers.Services;

public class CustomerService(IDbContextFactory<AppDbContext> contextFactory) : ICustomerService
{
    public async Task<List<Customer>> GetAllAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Customers.FindAsync(id);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Phone == phone);
    }

    public async Task<List<Customer>> SearchAsync(string query)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Customers.AsNoTracking()
            .Where(c => c.Name.Contains(query) || (c.Phone != null && c.Phone.Contains(query)))
            .OrderBy(c => c.Name)
            .Take(50)
            .ToListAsync();
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateAsync(Customer customer)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Customers.Update(customer);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var entity = await db.Customers.FindAsync(id);
        if (entity is not null)
        {
            db.Customers.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task AddLoyaltyPointsAsync(int customerId, int points)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is not null)
        {
            customer.LoyaltyPoints += points;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdatePurchaseStatsAsync(int customerId, decimal amount)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is not null)
        {
            customer.TotalPurchaseAmount += amount;
            customer.VisitCount++;
            await db.SaveChangesAsync();
        }
    }
}
