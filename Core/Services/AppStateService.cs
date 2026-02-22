using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
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
///   <item>Add <c>CurrentBillingSession</c> typed to a <c>BillingSession</c> model when billing is implemented.</item>
///   <item>Persist notifications to the database for cross-session history.</item>
///   <item>Add <c>ThemeName</c> / <c>CultureInfo</c> for runtime theming and localization.</item>
///   <item>Add <c>ConnectedDevices</c> collection for barcode scanners, receipt printers, etc.</item>
///   <item>Raise domain events (e.g., <c>UserLoggedIn</c>) via an event aggregator for loose coupling.</item>
/// </list>
/// </para>
/// </summary>
public partial class AppStateService : ObservableObject, IAppStateService
{
    private readonly DispatcherTimer _clockTimer;
    private readonly IRegionalSettingsService _regional;

    public AppStateService(IRegionalSettingsService regional)
    {
        _regional = regional;

        Notifications = [];
        Notifications.CollectionChanged += (_, _) =>
            OnPropertyChanged(nameof(UnreadNotificationCount));

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => CurrentTime = _regional.FormatTime(_regional.Now);
    }

    // ── Observable state ──

    [ObservableProperty]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial UserType CurrentUserType { get; set; }

    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; }

    [ObservableProperty]
    public partial string CurrentTime { get; set; } = string.Empty;

    [ObservableProperty]
    public partial object? CurrentBillingSession { get; set; }

    public ObservableCollection<AppNotification> Notifications { get; }

    public int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);

    // ── State mutations ──

    public void SetFirmInfo(string firmName) =>
        FirmName = firmName;

    public void SetCurrentUser(UserType userType) =>
        CurrentUserType = userType;

    public void SetLoggedIn(bool isLoggedIn)
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
    }

    public void SetBillingSession(object? session) =>
        CurrentBillingSession = session;

    public void AddNotification(AppNotification notification) =>
        Notifications.Add(notification);

    public void MarkNotificationRead(AppNotification notification)
    {
        notification.IsRead = true;
        OnPropertyChanged(nameof(UnreadNotificationCount));
    }

    public void ClearNotifications() =>
        Notifications.Clear();

    public void Reset()
    {
        FirmName = string.Empty;
        CurrentUserType = default;
        IsLoggedIn = false;
        CurrentBillingSession = null;
        Notifications.Clear();
    }
}
