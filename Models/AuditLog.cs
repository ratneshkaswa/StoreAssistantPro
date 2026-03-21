using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Immutable audit trail entry (#291). Captures all auditable actions:
/// sales, returns, price changes, discounts, login/logout, settings changes.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Action performed: SaleCompleted, ReturnProcessed, PriceOverride, DiscountGiven, Login, Logout, etc.</summary>
    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Entity type affected: Sale, Product, SaleReturn, User, AppConfig, etc.</summary>
    [Required, MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Primary key of the affected entity (stringified).</summary>
    [MaxLength(50)]
    public string? EntityId { get; set; }

    /// <summary>JSON snapshot of old values (for updates).</summary>
    public string? OldValue { get; set; }

    /// <summary>JSON snapshot of new values or action summary.</summary>
    public string? NewValue { get; set; }

    /// <summary>User or role who performed the action.</summary>
    [MaxLength(100)]
    public string? UserId { get; set; }

    /// <summary>Timestamp in IST.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Optional additional context or notes.</summary>
    [MaxLength(500)]
    public string? Details { get; set; }
}
