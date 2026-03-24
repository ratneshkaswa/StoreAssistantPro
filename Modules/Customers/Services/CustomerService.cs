using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Customers.Services;

public class CustomerService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : ICustomerService
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<Customer>> GetPagedAsync(PagedQuery query, string? search = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(c => c.Name.Contains(term) || (c.Phone != null && c.Phone.Contains(term)));
        }

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var items = await q
            .OrderBy(c => c.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<Customer>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        using var _ = perf.BeginScope("CustomerService.SearchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var term = query.Trim();
        return await context.Customers
            .AsNoTracking()
            .Where(c => c.Name.Contains(term) || (c.Phone != null && c.Phone.Contains(term)))
            .OrderBy(c => c.Name)
            .Take(50)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        using var _ = perf.BeginScope("CustomerService.GetByPhoneAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Phone == phone.Trim(), ct)
            .ConfigureAwait(false);
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(CustomerDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var gstinError = GstinValidator.Validate(dto.GSTIN);
        if (gstinError is not null)
            throw new ArgumentException(gstinError, nameof(dto.GSTIN));

        using var _ = perf.BeginScope("CustomerService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Duplicate phone detection
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var existing = await context.Customers
                .AnyAsync(c => c.Phone == dto.Phone.Trim(), ct)
                .ConfigureAwait(false);
            if (existing)
                throw new InvalidOperationException($"Customer with phone {dto.Phone} already exists.");
        }

        context.Customers.Add(new Customer
        {
            Name = dto.Name.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            GSTIN = dto.GSTIN?.Trim(),
            Notes = dto.Notes?.Trim(),
            Birthday = dto.Birthday,
            Anniversary = dto.Anniversary,
            CustomerGroup = dto.CustomerGroup?.Trim(),
            CreditLimit = dto.CreditLimit,
            IsActive = true,
            CreatedDate = regional.Now
        });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, CustomerDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var gstinError = GstinValidator.Validate(dto.GSTIN);
        if (gstinError is not null)
            throw new ArgumentException(gstinError, nameof(dto.GSTIN));

        using var _ = perf.BeginScope("CustomerService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer Id {id} not found.");

        // Duplicate phone detection on update
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var phoneTaken = await context.Customers
                .AnyAsync(c => c.Phone == dto.Phone.Trim() && c.Id != id, ct)
                .ConfigureAwait(false);
            if (phoneTaken)
                throw new InvalidOperationException($"Another customer already has phone {dto.Phone}.");
        }

        entity.Name = dto.Name.Trim();
        entity.Phone = dto.Phone?.Trim();
        entity.Email = dto.Email?.Trim();
        entity.Address = dto.Address?.Trim();
        entity.GSTIN = dto.GSTIN?.Trim();
        entity.Notes = dto.Notes?.Trim();
        entity.Birthday = dto.Birthday;
        entity.Anniversary = dto.Anniversary;
        entity.CustomerGroup = dto.CustomerGroup?.Trim();
        entity.CreditLimit = dto.CreditLimit;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> ImportBulkAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.ImportBulkAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existing = await context.Customers
            .AsNoTracking()
            .Select(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingNames = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var now = regional.Now;
        var count = 0;

        foreach (var row in rows)
        {
            var name = (row.GetValueOrDefault("Name") ?? row.GetValueOrDefault("Customer") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || !existingNames.Add(name))
                continue;

            context.Customers.Add(new Customer
            {
                Name = name,
                Phone = NullIfEmpty(row.GetValueOrDefault("Phone")),
                Email = NullIfEmpty(row.GetValueOrDefault("Email")),
                Address = NullIfEmpty(row.GetValueOrDefault("Address")),
                GSTIN = NullIfEmpty(row.GetValueOrDefault("GSTIN")),
                Notes = NullIfEmpty(row.GetValueOrDefault("Notes")),
                IsActive = true,
                CreatedDate = now
            });
            count++;
        }

        if (count > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return count;
    }

    public async Task<IReadOnlyList<CustomerPurchaseSummary>> GetPurchaseHistoryAsync(int customerId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetPurchaseHistoryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Sales
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new CustomerPurchaseSummary(
                s.Id,
                s.InvoiceNumber,
                s.SaleDate,
                s.TotalAmount,
                s.PaymentMethod,
                s.Items.Count))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int customerId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetOutstandingBalanceAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            .ConfigureAwait(false);

        if (customer is null || string.IsNullOrWhiteSpace(customer.Phone))
            return 0;

        return await context.Debtors
            .AsNoTracking()
            .Where(d => d.Phone == customer.Phone)
            .SumAsync(d => d.TotalAmount - d.PaidAmount, ct)
            .ConfigureAwait(false);
    }

    public async Task CollectPaymentAsync(int customerId, decimal amount, string paymentMethod, string? reference, CancellationToken ct = default)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive.", nameof(amount));

        using var _ = perf.BeginScope("CustomerService.CollectPaymentAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer Id {customerId} not found.");

        if (string.IsNullOrWhiteSpace(customer.Phone))
            throw new InvalidOperationException("Customer has no phone number for debtor lookup.");

        var debtors = await context.Debtors
            .Where(d => d.Phone == customer.Phone && d.PaidAmount < d.TotalAmount)
            .OrderBy(d => d.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var remaining = amount;
        foreach (var debtor in debtors)
        {
            if (remaining <= 0) break;
            var owed = debtor.TotalAmount - debtor.PaidAmount;
            var apply = Math.Min(owed, remaining);
            debtor.PaidAmount += apply;
            remaining -= apply;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ── Loyalty points (#162) ──

    public async Task AddLoyaltyPointsAsync(int customerId, int points, CancellationToken ct = default)
    {
        if (points <= 0) return;
        using var _ = perf.BeginScope("CustomerService.AddLoyaltyPointsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            .ConfigureAwait(false);
        if (entity is null) return;
        entity.LoyaltyPoints += points;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> RedeemLoyaltyPointsAsync(int customerId, int points, CancellationToken ct = default)
    {
        if (points <= 0) return false;
        using var _ = perf.BeginScope("CustomerService.RedeemLoyaltyPointsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            .ConfigureAwait(false);
        if (entity is null || entity.LoyaltyPoints < points) return false;
        entity.LoyaltyPoints -= points;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    // ── Tier auto-compute (#163) ──

    public async Task RecalculateTierAsync(int customerId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.RecalculateTierAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            .ConfigureAwait(false);
        if (entity is null) return;

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (config is null) return;

        entity.Tier = entity.TotalPurchaseAmount switch
        {
            var amt when amt >= config.PlatinumTierThreshold => "Platinum",
            var amt when amt >= config.GoldTierThreshold => "Gold",
            var amt when amt >= config.SilverTierThreshold => "Silver",
            _ => "Regular"
        };
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Birthday / Anniversary (#164, #165) ──

    public async Task<IReadOnlyList<Customer>> GetUpcomingBirthdaysAsync(int withinDays = 7, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetUpcomingBirthdaysAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var today = regional.Now.Date;
        var all = await context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive && c.Birthday != null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return all.Where(c =>
        {
            var bday = new DateTime(today.Year, c.Birthday!.Value.Month, c.Birthday.Value.Day);
            if (bday < today) bday = bday.AddYears(1);
            return (bday - today).TotalDays <= withinDays;
        }).OrderBy(c => c.Birthday!.Value.Month).ThenBy(c => c.Birthday!.Value.Day).ToList();
    }

    public async Task<IReadOnlyList<Customer>> GetUpcomingAnniversariesAsync(int withinDays = 7, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetUpcomingAnniversariesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var today = regional.Now.Date;
        var all = await context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive && c.Anniversary != null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return all.Where(c =>
        {
            var anniv = new DateTime(today.Year, c.Anniversary!.Value.Month, c.Anniversary.Value.Day);
            if (anniv < today) anniv = anniv.AddYears(1);
            return (anniv - today).TotalDays <= withinDays;
        }).OrderBy(c => c.Anniversary!.Value.Month).ThenBy(c => c.Anniversary!.Value.Day).ToList();
    }

    // ── Credit limit (#173) ──

    public async Task<bool> CanExtendCreditAsync(int customerId, decimal amount, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.CanExtendCreditAsync");
        var outstanding = await GetOutstandingBalanceAsync(customerId, ct).ConfigureAwait(false);
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            .ConfigureAwait(false);
        if (customer is null) return false;
        if (customer.CreditLimit <= 0) return true; // 0 = unlimited
        return (outstanding + amount) <= customer.CreditLimit;
    }

    // ── Customer statement (#450) ──

    public async Task<CustomerStatement> GetStatementAsync(int customerId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CustomerService.GetStatementAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer Id {customerId} not found.");

        var salesQuery = context.Sales
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId);
        if (from.HasValue) salesQuery = salesQuery.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue) salesQuery = salesQuery.Where(s => s.SaleDate <= to.Value.Date.AddDays(1));
        var sales = await salesQuery.OrderBy(s => s.SaleDate).ToListAsync(ct).ConfigureAwait(false);

        var paymentsQuery = context.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId);
        if (from.HasValue) paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue) paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= to.Value.Date.AddDays(1));
        var payments = await paymentsQuery.OrderBy(p => p.PaymentDate).ToListAsync(ct).ConfigureAwait(false);

        var lines = new List<CustomerStatementLine>();
        decimal running = 0;

        // Merge sales and payments chronologically
        var saleIndex = 0;
        var paymentIndex = 0;
        while (saleIndex < sales.Count || paymentIndex < payments.Count)
        {
            var nextSale = saleIndex < sales.Count ? sales[saleIndex] : null;
            var nextPayment = paymentIndex < payments.Count ? payments[paymentIndex] : null;

            if (nextSale is not null && (nextPayment is null || nextSale.SaleDate <= nextPayment.PaymentDate))
            {
                running += nextSale.TotalAmount;
                lines.Add(new CustomerStatementLine(nextSale.SaleDate, $"Sale {nextSale.InvoiceNumber}", nextSale.TotalAmount, 0, running));
                saleIndex++;
            }
            else if (nextPayment is not null)
            {
                running -= nextPayment.Amount;
                var paymentLabel = string.IsNullOrWhiteSpace(nextPayment.Note)
                    ? "Payment"
                    : $"Payment ({nextPayment.Note.Trim()})";
                lines.Add(new CustomerStatementLine(nextPayment.PaymentDate, paymentLabel, 0, nextPayment.Amount, running));
                paymentIndex++;
            }
        }

        var totalPurchases = sales.Sum(s => s.TotalAmount);
        var totalPayments = payments.Sum(p => p.Amount);

        return new CustomerStatement(
            customer.Name,
            customer.Phone,
            customer.GSTIN,
            customer.Address,
            totalPurchases,
            totalPayments,
            totalPurchases - totalPayments,
            lines);
    }
}
