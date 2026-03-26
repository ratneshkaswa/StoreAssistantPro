using System.Media;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Subscribes to the shared notification pipeline and forwards notifications
/// to the Windows Notification Center presenter.
/// </summary>
public sealed class WindowsNotificationBridge : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IWindowsNotificationPresenter _presenter;
    private readonly ILogger<WindowsNotificationBridge> _logger;
    private bool _disposed;

    public WindowsNotificationBridge(
        IEventBus eventBus,
        IWindowsNotificationPresenter presenter,
        ILogger<WindowsNotificationBridge> logger)
    {
        _eventBus = eventBus;
        _presenter = presenter;
        _logger = logger;

        _presenter.EnsureRegistered();
        _eventBus.Subscribe<NotificationPostedEvent>(OnNotificationPostedAsync);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _eventBus.Unsubscribe<NotificationPostedEvent>(OnNotificationPostedAsync);
        _disposed = true;
    }

    private Task OnNotificationPostedAsync(NotificationPostedEvent e)
    {
        var preferences = UserPreferencesStore.GetSnapshot();
        if (!preferences.WindowsNotificationsEnabled || !UserPreferencesStore.MeetsNotificationThreshold(e.Notification.Level))
            return Task.CompletedTask;

        try
        {
            TryPlayNotificationSound(e.Notification.Level, preferences.NotificationSoundEnabled);
            _presenter.TryShow(e.Notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to forward notification to Windows Notification Center");
        }

        return Task.CompletedTask;
    }

    private static void TryPlayNotificationSound(AppNotificationLevel level, bool soundEnabled)
    {
        if (!soundEnabled)
            return;

        if (AppDomain.CurrentDomain.FriendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            GetSystemSound(level).Play();
        }
        catch
        {
            // Best-effort only. Native toast delivery remains the primary notification surface.
        }
    }

    private static SystemSound GetSystemSound(AppNotificationLevel level) => level switch
    {
        AppNotificationLevel.Error => SystemSounds.Hand,
        AppNotificationLevel.Warning => SystemSounds.Exclamation,
        AppNotificationLevel.Success => SystemSounds.Asterisk,
        _ => SystemSounds.Asterisk
    };
}
