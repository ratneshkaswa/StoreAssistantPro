using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Lightweight toast notification item displayed by <c>ToastHost</c>.
/// Each toast auto-removes after its lifetime expires.
/// </summary>
public sealed class ToastItem
{
    /// <summary>Unique identifier for removal tracking.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Short toast message.</summary>
    public required string Message { get; init; }

    /// <summary>Visual severity level (drives icon + color).</summary>
    public AppNotificationLevel Level { get; init; } = AppNotificationLevel.Info;

    /// <summary>When the toast was created (for display ordering).</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
