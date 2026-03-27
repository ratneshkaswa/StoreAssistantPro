using System.Collections.ObjectModel;
using System.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Single source of truth for application-wide state.
/// ViewModels should read state and bind to properties.
/// Only services should call mutation methods.
/// </summary>
public interface IAppStateService : INotifyPropertyChanged
{
    // ── Observable state (read by ViewModels, bound in XAML) ──

    string FirmName { get; }
    UserType CurrentUserType { get; }
    UserType? LastLoggedInUserType { get; }
    bool IsLoggedIn { get; }
    string CurrentTime { get; }
    OperationalMode CurrentMode { get; }
    BillingSessionState CurrentBillingSession { get; }
    ObservableCollection<AppNotification> Notifications { get; }
    int UnreadNotificationCount { get; }

    /// <summary>
    /// <c>true</c> when the database is unreachable. Driven by
    /// <see cref="Events.ConnectionLostEvent"/> /
    /// <see cref="Events.ConnectionRestoredEvent"/>.
    /// </summary>
    bool IsOfflineMode { get; }

    /// <summary>
    /// <c>true</c> when Smart Tooltips are enabled application-wide.
    /// Toggled from System Settings → General.
    /// </summary>
    bool SmartTooltipsEnabled { get; }

    /// <summary>
    /// <c>true</c> when the admin PIN is still the factory default ("1234").
    /// Used to show a warning banner until the admin changes the PIN.
    /// </summary>
    bool IsDefaultAdminPin { get; }

    /// <summary>
    /// <c>true</c> until the minimum first-run setup has been completed.
    /// Used to route first login into firm setup instead of the workspace.
    /// </summary>
    bool IsInitialSetupPending { get; }

    /// <summary>
    /// Timestamp of the most recent connectivity health check.
    /// </summary>
    DateTime? LastConnectionCheck { get; }

    // ── State mutations (called by services only) ──

    void SetFirmInfo(string firmName);
    void SetCurrentUser(UserType userType);
    void SetLoggedIn(bool isLoggedIn);
    void SetMode(OperationalMode mode);
    void SetBillingSession(BillingSessionState session);

    /// <summary>
    /// Updates the connectivity state. Called by
    /// <see cref="IConnectivityMonitorService"/> when the database
    /// status changes.
    /// </summary>
    void SetConnectivity(bool isOffline, DateTime checkTime);
    void SetSmartTooltipsEnabled(bool enabled);
    void SetDefaultPinFlag(bool isDefault);
    void SetInitialSetupPending(bool pending);

    void AddNotification(AppNotification notification);
    void MarkNotificationRead(AppNotification notification);
    void ClearNotifications();
    void Reset();
}
