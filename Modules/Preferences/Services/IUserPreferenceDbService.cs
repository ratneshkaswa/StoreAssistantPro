using StoreAssistantPro.Models.Preferences;

namespace StoreAssistantPro.Modules.Preferences.Services;

/// <summary>
/// Per-user preference persistence service (#898-916).
/// </summary>
public interface IUserPreferenceDbService
{
    /// <summary>Get a preference value for a user.</summary>
    Task<string?> GetPreferenceAsync(int userId, string key, CancellationToken ct = default);

    /// <summary>Set a preference value for a user.</summary>
    Task SetPreferenceAsync(int userId, string key, string? value, CancellationToken ct = default);

    /// <summary>Get all preferences for a user.</summary>
    Task<IReadOnlyDictionary<string, string?>> GetAllPreferencesAsync(int userId, CancellationToken ct = default);

    /// <summary>Delete a preference for a user.</summary>
    Task DeletePreferenceAsync(int userId, string key, CancellationToken ct = default);

    /// <summary>Get the user's default landing page (#898).</summary>
    Task<string?> GetDefaultLandingPageAsync(int userId, CancellationToken ct = default);

    /// <summary>Get the user's default view mode (list/grid) (#899).</summary>
    Task<string?> GetDefaultViewModeAsync(int userId, CancellationToken ct = default);

    /// <summary>Get items per page preference (#900).</summary>
    Task<int> GetItemsPerPageAsync(int userId, int fallback = 20, CancellationToken ct = default);

    /// <summary>Get the user's default printer (#901).</summary>
    Task<string?> GetDefaultPrinterAsync(int userId, CancellationToken ct = default);

    /// <summary>Get the user's default payment method (#902).</summary>
    Task<string?> GetDefaultPaymentMethodAsync(int userId, CancellationToken ct = default);

    /// <summary>Get the user's sidebar state (expanded/collapsed) (#903).</summary>
    Task<bool> GetSidebarExpandedAsync(int userId, CancellationToken ct = default);

    /// <summary>Get column visibility settings for a view (#904).</summary>
    Task<IReadOnlyDictionary<string, bool>> GetColumnVisibilityAsync(int userId, string viewName, CancellationToken ct = default);

    /// <summary>Save column visibility settings for a view (#904).</summary>
    Task SetColumnVisibilityAsync(int userId, string viewName, IReadOnlyDictionary<string, bool> columns, CancellationToken ct = default);

    /// <summary>Get sort preference for a view (#905).</summary>
    Task<(string Column, bool Ascending)?> GetSortPreferenceAsync(int userId, string viewName, CancellationToken ct = default);

    /// <summary>Save sort preference for a view (#905).</summary>
    Task SetSortPreferenceAsync(int userId, string viewName, string column, bool ascending, CancellationToken ct = default);

    /// <summary>Get filter preference for a view (#906).</summary>
    Task<string?> GetFilterPreferenceAsync(int userId, string viewName, CancellationToken ct = default);

    /// <summary>Save filter preference for a view (#906).</summary>
    Task SetFilterPreferenceAsync(int userId, string viewName, string filterJson, CancellationToken ct = default);

    /// <summary>Check if a notification channel is enabled for user (#907-909).</summary>
    Task<bool> IsNotificationEnabledAsync(int userId, string channel, CancellationToken ct = default);

    /// <summary>Set notification channel enabled state (#907-909).</summary>
    Task SetNotificationEnabledAsync(int userId, string channel, bool enabled, CancellationToken ct = default);

    /// <summary>Get alert sound preference (#910).</summary>
    Task<string?> GetAlertSoundAsync(int userId, CancellationToken ct = default);

    /// <summary>Get personal low stock alert threshold (#911).</summary>
    Task<int?> GetLowStockThresholdAsync(int userId, CancellationToken ct = default);

    /// <summary>Get quiet hours configuration (#914).</summary>
    Task<QuietHoursConfig?> GetQuietHoursAsync(int userId, CancellationToken ct = default);

    /// <summary>Set quiet hours configuration (#914).</summary>
    Task SetQuietHoursAsync(int userId, QuietHoursConfig config, CancellationToken ct = default);

    /// <summary>Check if current time is within quiet hours (#914).</summary>
    Task<bool> IsInQuietHoursAsync(int userId, CancellationToken ct = default);

    /// <summary>Get notification priority filter (#915).</summary>
    Task<string?> GetNotificationPriorityFilterAsync(int userId, CancellationToken ct = default);

    /// <summary>Record a dismissed notification (#916).</summary>
    Task RecordDismissedNotificationAsync(int userId, NotificationHistoryEntry entry, CancellationToken ct = default);

    /// <summary>Get notification history (#916).</summary>
    Task<IReadOnlyList<NotificationHistoryEntry>> GetNotificationHistoryAsync(int userId, int maxResults = 50, CancellationToken ct = default);
}
