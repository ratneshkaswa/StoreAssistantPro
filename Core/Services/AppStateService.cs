using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton application state container. Extends <see cref="ObservableObject"/>
/// so WPF bindings update automatically when state changes.
/// <para>
/// <b>Architecture rule:</b> ViewModels read properties; only services call mutation methods.
/// </para>
/// <para>
/// <b>Future expansion points:</b>
/// <list type="bullet">
///   <item>Persist notifications to the database for cross-session history.</item>
///   <item>Add <c>ThemeName</c> / <c>CultureInfo</c> for runtime theming and localization.</item>
///   <item>Add <c>ConnectedDevices</c> collection for barcode scanners, receipt printers, etc.</item>
///   <item>Raise domain events (e.g., <c>UserLoggedIn</c>) via an event aggregator for loose coupling.</item>
/// </list>
/// </para>
/// </summary>
public partial class AppStateService : ObservableObject, IAppStateService, IDisposable
{
    private readonly DispatcherTimer _clockTimer;
    private readonly Dispatcher? _dispatcher;
    private readonly IRegionalSettingsService _regional;
    private readonly HashSet<AppNotification> _trackedNotifications = [];
    private int _unreadCount;
    private bool _disposed;

    public AppStateService(IRegionalSettingsService regional, Dispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? Application.Current?.Dispatcher;
        _regional = regional;

        SmartTooltipsEnabled = false;
        SmartTooltip.GlobalEnabled = false;

        Notifications = [];
        Notifications.CollectionChanged += OnNotificationsChanged;

        _clockTimer = _dispatcher is not null
            ? new DispatcherTimer(DispatcherPriority.Normal, _dispatcher)
            : new DispatcherTimer();
        _clockTimer.Interval = TimeSpan.FromSeconds(1);
        _clockTimer.Tick += OnClockTimerTick;
    }

    private void OnClockTimerTick(object? sender, EventArgs e) =>
        CurrentTime = _regional.FormatTime(_regional.Now);

    private void OnNotificationsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                TrackNotifications(e.NewItems);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                UntrackNotifications(e.OldItems);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                UntrackNotifications(e.OldItems);
                TrackNotifications(e.NewItems);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                UntrackAllNotifications();
                break;
        }

        UpdateUnreadNotificationCount();
    }

    private void TrackNotifications(System.Collections.IList? notifications)
    {
        if (notifications is null)
            return;

        foreach (var notification in notifications.OfType<AppNotification>())
        {
            if (_trackedNotifications.Add(notification))
                notification.PropertyChanged += OnNotificationPropertyChanged;
        }
    }

    private void UntrackNotifications(System.Collections.IList? notifications)
    {
        if (notifications is null)
            return;

        foreach (var notification in notifications.OfType<AppNotification>())
        {
            if (_trackedNotifications.Remove(notification))
                notification.PropertyChanged -= OnNotificationPropertyChanged;
        }
    }

    private void UntrackAllNotifications()
    {
        foreach (var notification in _trackedNotifications)
            notification.PropertyChanged -= OnNotificationPropertyChanged;

        _trackedNotifications.Clear();
    }

    private void OnNotificationPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppNotification.IsRead))
            UpdateUnreadNotificationCount();
    }

    private void UpdateUnreadNotificationCount()
    {
        var unreadCount = Notifications.Count(notification => !notification.IsRead);
        if (_unreadCount == unreadCount)
            return;

        _unreadCount = unreadCount;
        OnPropertyChanged(nameof(UnreadNotificationCount));
    }

    // ── Observable state ──

    [ObservableProperty]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial UserType CurrentUserType { get; set; }

    [ObservableProperty]
    public partial UserType? LastLoggedInUserType { get; set; }

    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; }

    [ObservableProperty]
    public partial string CurrentTime { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BillingSessionState CurrentBillingSession { get; set; }

    [ObservableProperty]
    public partial OperationalMode CurrentMode { get; set; }

    [ObservableProperty]
    public partial bool IsOfflineMode { get; set; }

    [ObservableProperty]
    public partial bool SmartTooltipsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsDefaultAdminPin { get; set; }

    [ObservableProperty]
    public partial bool IsInitialSetupPending { get; set; }

    [ObservableProperty]
    public partial DateTime? LastConnectionCheck { get; set; }

    public ObservableCollection<AppNotification> Notifications { get; }

    public int UnreadNotificationCount => _unreadCount;

    // ── State mutations ──

    public void SetFirmInfo(string firmName) =>
        RunOnDispatcher(() => FirmName = firmName);

    public void SetCurrentUser(UserType userType)
    {
        RunOnDispatcher(() =>
        {
            CurrentUserType = userType;
            LastLoggedInUserType = userType;
        });
    }

    public void SetLoggedIn(bool isLoggedIn)
    {
        RunOnDispatcher(() =>
        {
            IsLoggedIn = isLoggedIn;

            // Start the clock on first login; stop when logged out.
            if (isLoggedIn && !_clockTimer.IsEnabled)
            {
                CurrentTime = _regional.FormatTime(_regional.Now);
                _clockTimer.Start();
            }
            else if (!isLoggedIn && _clockTimer.IsEnabled)
            {
                _clockTimer.Stop();
            }
        });
    }

    public void SetBillingSession(BillingSessionState session) =>
        RunOnDispatcher(() => CurrentBillingSession = session);

    public void SetMode(OperationalMode mode) =>
        RunOnDispatcher(() => CurrentMode = mode);

    public void SetConnectivity(bool isOffline, DateTime checkTime)
    {
        RunOnDispatcher(() =>
        {
            IsOfflineMode = isOffline;
            LastConnectionCheck = checkTime;
        });
    }

    public void SetSmartTooltipsEnabled(bool enabled)
    {
        RunOnDispatcher(() =>
        {
            SmartTooltipsEnabled = enabled;
            SmartTooltip.GlobalEnabled = enabled;
        });
    }

    public void SetDefaultPinFlag(bool isDefault)
    {
        RunOnDispatcher(() => IsDefaultAdminPin = isDefault);
    }

    public void SetInitialSetupPending(bool pending)
    {
        RunOnDispatcher(() => IsInitialSetupPending = pending);
    }

    public void AddNotification(AppNotification notification) =>
        RunOnDispatcher(() => Notifications.Add(notification));

    public void MarkNotificationRead(AppNotification notification)
    {
        RunOnDispatcher(() =>
        {
            if (!notification.IsRead)
                notification.IsRead = true;
        });
    }

    public void ClearNotifications() =>
        RunOnDispatcher(() => Notifications.Clear());

    public void Reset()
    {
        RunOnDispatcher(() =>
        {
            FirmName = string.Empty;
            CurrentUserType = default;
            IsLoggedIn = false;
            CurrentMode = OperationalMode.Management;
            CurrentBillingSession = BillingSessionState.None;
            IsOfflineMode = false;
            LastConnectionCheck = null;
            SmartTooltipsEnabled = false;
            SmartTooltip.GlobalEnabled = false;
            IsDefaultAdminPin = false;
            Notifications.Clear();
        });
    }

    private void RunOnDispatcher(Action action)
    {
        if (_dispatcher is null
            || _dispatcher.HasShutdownStarted
            || _dispatcher.HasShutdownFinished)
        {
            action();
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.Invoke(action);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        RunOnDispatcher(() =>
        {
            _clockTimer.Stop();
            _clockTimer.Tick -= OnClockTimerTick;
            Notifications.CollectionChanged -= OnNotificationsChanged;
            UntrackAllNotifications();
        });

        GC.SuppressFinalize(this);
    }
}
