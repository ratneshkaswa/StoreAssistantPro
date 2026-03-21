using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Immutable audit trail implementation (#291).
/// Uses short-lived DbContext per operation.
/// </summary>
public class AuditService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional) : IAuditService
{
    public async Task LogAsync(string action, string entityType, string? entityId,
        string? oldValue, string? newValue, string? userId, string? details = null,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            UserId = userId,
            Timestamp = regional.Now,
            Details = details
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task LogSaleCompletedAsync(Sale sale, string? cashierRole, CancellationToken ct = default)
    {
        var newValue = $"Invoice: {sale.InvoiceNumber}, Total: {sale.TotalAmount:F2}, " +
                       $"Payment: {sale.PaymentMethod}, Items: {sale.Items.Count}";

        return LogAsync("SaleCompleted", "Sale", sale.Id.ToString(),
            null, newValue, cashierRole,
            sale.CustomerId.HasValue ? $"Customer #{sale.CustomerId}" : "Walk-in", ct);
    }

    public Task LogPriceOverrideAsync(int productId, string productName,
        decimal oldPrice, decimal newPrice, string? userId, CancellationToken ct = default)
    {
        return LogAsync("PriceOverride", "Product", productId.ToString(),
            $"SalePrice: {oldPrice:F2}", $"SalePrice: {newPrice:F2}", userId,
            $"Product: {productName}", ct);
    }

    public Task LogDiscountAsync(int saleId, string invoiceNumber,
        string discountType, decimal discountValue, decimal discountAmount,
        string? approvedBy, CancellationToken ct = default)
    {
        var newValue = $"Type: {discountType}, Value: {discountValue}, Amount: {discountAmount:F2}";

        return LogAsync("DiscountGiven", "Sale", saleId.ToString(),
            null, newValue, approvedBy,
            $"Invoice: {invoiceNumber}", ct);
    }

    public Task LogReturnAsync(SaleReturn saleReturn, string? userId, CancellationToken ct = default)
    {
        var newValue = $"Return #{saleReturn.ReturnNumber}, Refund: {saleReturn.RefundAmount:F2}, " +
                       $"Reason: {saleReturn.Reason}";

        return LogAsync("ReturnProcessed", "SaleReturn", saleReturn.Id.ToString(),
            null, newValue, userId,
            $"Original Sale #{saleReturn.SaleId}", ct);
    }

    public Task LogLoginAsync(string userId, CancellationToken ct = default)
    {
        return LogAsync("Login", "User", null, null, null, userId, ct: ct);
    }

    public Task LogLogoutAsync(string userId, CancellationToken ct = default)
    {
        return LogAsync("Logout", "User", null, null, null, userId, ct: ct);
    }

    public Task LogSettingsChangedAsync(string settingName, string? oldValue, string? newValue,
        string? userId, CancellationToken ct = default)
    {
        return LogAsync("SettingsChanged", "AppConfig", null,
            $"{settingName}: {oldValue}", $"{settingName}: {newValue}", userId, ct: ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetLogsAsync(
        string? action = null, string? entityType = null,
        DateTime? from = null, DateTime? to = null,
        string? userId = null, int maxResults = 100,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(entityType))
            q = q.Where(a => a.EntityType == entityType);

        if (from.HasValue)
            q = q.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            q = q.Where(a => a.Timestamp <= to.Value);

        if (!string.IsNullOrWhiteSpace(userId))
            q = q.Where(a => a.UserId == userId);

        return await q
            .OrderByDescending(a => a.Timestamp)
            .Take(maxResults)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
