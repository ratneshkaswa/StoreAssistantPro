namespace StoreAssistantPro.Models.Preferences;

/// <summary>
/// Per-user preference settings (#898-916).
/// </summary>
public sealed class UserPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Well-known preference keys matching features #898-916.
/// </summary>
public static class PreferenceKeys
{
    public const string DefaultLandingPage = "DefaultLandingPage";        // #898
    public const string DefaultViewMode = "DefaultViewMode";              // #899
    public const string ItemsPerPage = "ItemsPerPage";                    // #900
    public const string DefaultPrinter = "DefaultPrinter";                // #901
    public const string DefaultPaymentMethod = "DefaultPaymentMethod";    // #902
    public const string SidebarState = "SidebarState";                    // #903
    public const string ColumnVisibility = "ColumnVisibility";            // #904
    public const string SortPreference = "SortPreference";                // #905
    public const string FilterPreference = "FilterPreference";            // #906
    public const string EmailNotifications = "EmailNotifications";        // #907
    public const string SmsNotifications = "SmsNotifications";            // #908
    public const string InAppNotifications = "InAppNotifications";        // #909
    public const string AlertSound = "AlertSound";                        // #910
    public const string LowStockThreshold = "LowStockThreshold";          // #911
    public const string DailyReportAutoEmail = "DailyReportAutoEmail";    // #912
    public const string WeeklySummary = "WeeklySummary";                  // #913
    public const string QuietHoursStart = "QuietHoursStart";              // #914
    public const string QuietHoursEnd = "QuietHoursEnd";                  // #914
    public const string NotificationPriority = "NotificationPriority";    // #915
    public const string NotificationHistory = "NotificationHistory";      // #916
}

/// <summary>
/// Notification quiet hours configuration (#914).
/// </summary>
public sealed record QuietHoursConfig(
    TimeSpan Start,
    TimeSpan End,
    bool IsEnabled);

/// <summary>
/// Dismissed notification record for history (#916).
/// </summary>
public sealed record NotificationHistoryEntry(
    string Title,
    string Message,
    string Level,
    DateTime OccurredAt,
    DateTime DismissedAt);
