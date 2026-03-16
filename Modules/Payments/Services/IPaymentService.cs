using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Payments.Services;

public interface IPaymentService
{
    Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByCustomerAsync(int customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken ct = default);
    Task CreateAsync(PaymentDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, PaymentDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<PaymentStats> GetStatsAsync(CancellationToken ct = default);
}

public record PaymentDto(
    int CustomerId,
    DateTime PaymentDate,
    decimal Amount,
    string? Note);

public record PaymentStats(
    int TotalPayments,
    decimal TotalAmount);
