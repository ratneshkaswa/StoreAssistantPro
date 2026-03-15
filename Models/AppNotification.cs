using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Models;

public partial class AppNotification : ObservableObject
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required DateTime Timestamp { get; init; }
    public AppNotificationLevel Level { get; init; } = AppNotificationLevel.Info;

    [ObservableProperty]
    public partial bool IsRead { get; set; }
}

public enum AppNotificationLevel
{
    Info,
    Warning,
    Error,
    Success
}
