using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

/// <summary>
/// Self-contained ViewModel for the shell summary strip.
/// Subscribes to <see cref="IAppStateService"/> property changes so
/// user, connectivity, and clock state stay live without manual refresh.
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IAppStateService _appState;
    private readonly IEventBus _eventBus;
    private readonly IDashboardService _dashboardService;
    private readonly IRegionalSettingsService _regional;

    public DashboardViewModel(
        IAppStateService appState,
        IEventBus eventBus,
        IDashboardService dashboardService,
        IRegionalSettingsService regional)
    {
        _appState = appState;
        _eventBus = eventBus;
        _dashboardService = dashboardService;
        _regional = regional;

        _appState.PropertyChanged += OnAppStatePropertyChanged;
    }

    // Current user

    public string CurrentUser => CurrentUserLabel;
    public string CurrentUserLabel => _appState.CurrentUserType.ToString();
    public string CurrentUserInitials => BuildInitials(CurrentUserLabel);
    public string Greeting => BuildGreeting(CurrentUserLabel, _regional.Now);
    public UserType CurrentUserType => _appState.CurrentUserType;

    // Clock

    public string CurrentTime => _appState.CurrentTime;

    // Connectivity

    public bool IsOfflineMode => _appState.IsOfflineMode;
    public string ConnectionStatusText => IsOfflineMode ? "Offline" : "Connected";
    public string ConnectionStatusDetail => BuildConnectionStatusDetail(_appState.LastConnectionCheck, IsOfflineMode, _regional.Now);

    // Live update wiring

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IAppStateService.CurrentUserType):
                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(CurrentUserLabel));
                OnPropertyChanged(nameof(CurrentUserInitials));
                OnPropertyChanged(nameof(Greeting));
                OnPropertyChanged(nameof(CurrentUserType));
                break;
            case nameof(IAppStateService.CurrentTime):
                OnPropertyChanged(nameof(CurrentTime));
                OnPropertyChanged(nameof(Greeting));
                OnPropertyChanged(nameof(ConnectionStatusDetail));
                break;
            case nameof(IAppStateService.IsOfflineMode):
                OnPropertyChanged(nameof(IsOfflineMode));
                OnPropertyChanged(nameof(ConnectionStatusText));
                OnPropertyChanged(nameof(ConnectionStatusDetail));
                break;
            case nameof(IAppStateService.LastConnectionCheck):
                OnPropertyChanged(nameof(ConnectionStatusDetail));
                break;
        }
    }

    // Cleanup

    public void Dispose()
    {
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
    }

    private static string BuildInitials(string value)
    {
        var initials = value
            .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]))
            .Take(2)
            .ToArray();

        return initials.Length == 0 ? "SA" : new string(initials);
    }

    private static string BuildGreeting(string currentUserLabel, DateTime now)
    {
        var prefix = now.Hour switch
        {
            >= 5 and < 12 => "Good morning",
            >= 12 and < 17 => "Good afternoon",
            >= 17 and < 22 => "Good evening",
            _ => "Welcome back"
        };

        return $"{prefix}, {currentUserLabel}";
    }

    private static string BuildConnectionStatusDetail(DateTime? lastConnectionCheck, bool isOfflineMode, DateTime now)
    {
        if (lastConnectionCheck is null)
            return isOfflineMode ? "Connection unavailable" : "Waiting for first sync";

        var relative = FormatRelative(now - lastConnectionCheck.Value);
        return isOfflineMode
            ? $"Last check {relative}"
            : $"Checked {relative}";
    }

    private static string FormatRelative(TimeSpan delta)
    {
        if (delta < TimeSpan.FromMinutes(1))
            return "just now";

        if (delta < TimeSpan.FromHours(1))
            return $"{Math.Max(1, (int)delta.TotalMinutes)} min ago";

        if (delta < TimeSpan.FromDays(1))
            return $"{Math.Max(1, (int)delta.TotalHours)} hr ago";

        var days = Math.Max(1, (int)delta.TotalDays);
        return days == 1 ? "1 day ago" : $"{days} days ago";
    }
}
