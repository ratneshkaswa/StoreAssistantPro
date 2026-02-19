namespace StoreAssistantPro.Models;

public class AppNotification
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public AppNotificationLevel Level { get; init; } = AppNotificationLevel.Info;
    public bool IsRead { get; set; }
}

public enum AppNotificationLevel
{
    Info,
    Warning,
    Error,
    Success
}
