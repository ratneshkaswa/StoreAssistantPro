using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Preferences.Events;

/// <summary>Published when a user preference is changed.</summary>
public sealed class PreferenceChangedEvent(int userId, string key, string? newValue) : IEvent
{
    public int UserId { get; } = userId;
    public string Key { get; } = key;
    public string? NewValue { get; } = newValue;
}

/// <summary>Published when quiet hours state changes (enters or exits).</summary>
public sealed class QuietHoursStateChangedEvent(int userId, bool isInQuietHours) : IEvent
{
    public int UserId { get; } = userId;
    public bool IsInQuietHours { get; } = isInQuietHours;
}
