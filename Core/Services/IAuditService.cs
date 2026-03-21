using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Immutable audit trail service (#291).
/// Provides fire-and-forget logging for auditable business actions.
/// </summary>
public interface IAuditService
{
    /// <summary>Log a generic audit entry.</summary>
    Task LogAsync(string action, string entityType, string? entityId,
        string? oldValue, string? newValue, string? userId, string? details = null,
        CancellationToken ct = default);

    /// <summary>Log sale completion (#293).</summary>
    Task LogSaleCompletedAsync(Sale sale, string? cashierRole, CancellationToken ct = default);

    /// <summary>Log price override during billing (#294).</summary>
    Task LogPriceOverrideAsync(int productId, string productName,
        decimal oldPrice, decimal newPrice, string? userId, CancellationToken ct = default);

    /// <summary>Log discount given (#295).</summary>
    Task LogDiscountAsync(int saleId, string invoiceNumber,
        string discountType, decimal discountValue, decimal discountAmount,
        string? approvedBy, CancellationToken ct = default);

    /// <summary>Log return processed (#296).</summary>
    Task LogReturnAsync(SaleReturn saleReturn, string? userId, CancellationToken ct = default);

    /// <summary>Log login event (#297).</summary>
    Task LogLoginAsync(string userId, CancellationToken ct = default);

    /// <summary>Log logout event (#297).</summary>
    Task LogLogoutAsync(string userId, CancellationToken ct = default);

    /// <summary>Log settings changed (#298).</summary>
    Task LogSettingsChangedAsync(string settingName, string? oldValue, string? newValue,
        string? userId, CancellationToken ct = default);

    /// <summary>Query audit entries with filtering (#299).</summary>
    Task<IReadOnlyList<AuditLog>> GetLogsAsync(
        string? action = null, string? entityType = null,
        DateTime? from = null, DateTime? to = null,
        string? userId = null, int maxResults = 100,
        CancellationToken ct = default);
}
