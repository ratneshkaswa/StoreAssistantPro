using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

/// <summary>
/// Self-contained ViewModel for the status bar's dashboard summary strip.
/// Subscribes to <see cref="IAppStateService"/> property changes so
/// every metric updates live without manual refresh.
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IAppStateService _appState;
    private readonly IEventBus _eventBus;
    private readonly IDashboardService _dashboardService;

    public DashboardViewModel(
        IAppStateService appState,
        IEventBus eventBus,
        IDashboardService dashboardService)
    {
        _appState = appState;
        _eventBus = eventBus;
        _dashboardService = dashboardService;

        _appState.PropertyChanged += OnAppStatePropertyChanged;
    }

    // ── Current user ──

    public string CurrentUser => $"👤 {_appState.CurrentUserType}";
    public UserType CurrentUserType => _appState.CurrentUserType;

    // ── Clock ──

    public string CurrentTime => _appState.CurrentTime;

    // ── Connectivity ──

    public bool IsOfflineMode => _appState.IsOfflineMode;

    // ── Live update wiring ──

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IAppStateService.CurrentUserType):
                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(CurrentUserType));
                break;
            case nameof(IAppStateService.CurrentTime):
                OnPropertyChanged(nameof(CurrentTime));
                break;
            case nameof(IAppStateService.IsOfflineMode):
                OnPropertyChanged(nameof(IsOfflineMode));
                break;
        }
    }

    // ── Cleanup ──

    public void Dispose()
    {
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
    }
}
