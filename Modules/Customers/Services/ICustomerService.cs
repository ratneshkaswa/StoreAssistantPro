using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Customers.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<Customer>> GetPagedAsync(PagedQuery query, string? search = null, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> SearchAsync(string query, CancellationToken ct = default);
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(CustomerDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, CustomerDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);
    Task<int> ImportBulkAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerPurchaseSummary>> GetPurchaseHistoryAsync(int customerId, CancellationToken ct = default);
    Task<decimal> GetOutstandingBalanceAsync(int customerId, CancellationToken ct = default);
    Task CollectPaymentAsync(int customerId, decimal amount, string paymentMethod, string? reference, CancellationToken ct = default);

    // ── Loyalty points (#162) ──
    Task AddLoyaltyPointsAsync(int customerId, int points, CancellationToken ct = default);
    Task<bool> RedeemLoyaltyPointsAsync(int customerId, int points, CancellationToken ct = default);

    // ── Tier auto-compute (#163) ──
    Task RecalculateTierAsync(int customerId, CancellationToken ct = default);

    // ── Birthday / Anniversary (#164, #165) ──
    Task<IReadOnlyList<Customer>> GetUpcomingBirthdaysAsync(int withinDays = 7, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetUpcomingAnniversariesAsync(int withinDays = 7, CancellationToken ct = default);

    // ── Credit limit check (#173) ──
    Task<bool> CanExtendCreditAsync(int customerId, decimal amount, CancellationToken ct = default);

    // ── Customer statement print (#450) ──
    Task<CustomerStatement> GetStatementAsync(int customerId, DateTime? from, DateTime? to, CancellationToken ct = default);
}

public record CustomerDto(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? GSTIN,
    string? Notes,
    DateTime? Birthday = null,
    DateTime? Anniversary = null,
    string? CustomerGroup = null,
    decimal CreditLimit = 0);

public record CustomerPurchaseSummary(
    int SaleId,
    string InvoiceNumber,
    DateTime SaleDate,
    decimal TotalAmount,
    string PaymentMethod,
    int ItemCount);

/// <summary>Customer outstanding balance statement for printing (#450).</summary>
public record CustomerStatement(
    string CustomerName,
    string? Phone,
    string? GSTIN,
    string? Address,
    decimal TotalPurchases,
    decimal TotalPayments,
    decimal OutstandingBalance,
    IReadOnlyList<CustomerStatementLine> Lines);

public record CustomerStatementLine(
    DateTime Date,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);
