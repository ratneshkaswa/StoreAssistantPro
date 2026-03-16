using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Orders.Services;

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(OrderDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, OrderDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SetStatusAsync(int id, string status, CancellationToken ct = default);
    Task<OrderStats> GetStatsAsync(CancellationToken ct = default);
}

public record OrderDto(
    DateTime Date,
    string CustomerName,
    string? ItemDescription,
    int Quantity,
    decimal Rate,
    decimal Amount,
    DateTime? DeliveryDate);

public record OrderStats(
    int Pending,
    int Active,
    int Delivered,
    int Total);
