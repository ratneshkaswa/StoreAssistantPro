using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Navigation;
using StoreAssistantPro.Session;

namespace StoreAssistantPro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    public partial ObservableObject CurrentView { get; set; }

    [ObservableProperty]
    public partial string FirmName { get; set; }

    [ObservableProperty]
    public partial string CurrentUserDisplay { get; set; }

    [ObservableProperty]
    public partial bool IsLoggingOut { get; set; }

    public Action? RequestClose { get; set; }

    public MainViewModel(INavigationService navigationService, ISessionService sessionService)
    {
        _navigationService = navigationService;
        _sessionService = sessionService;

        FirmName = _sessionService.FirmName;
        CurrentUserDisplay = _sessionService.CurrentUserType.ToString();

        _navigationService.NavigateTo<DashboardViewModel>();
        CurrentView = _navigationService.CurrentView;
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        _navigationService.NavigateTo<DashboardViewModel>();
        CurrentView = _navigationService.CurrentView;
    }

    [RelayCommand]
    private void NavigateToProducts()
    {
        _navigationService.NavigateTo<ProductsViewModel>();
        CurrentView = _navigationService.CurrentView;
    }

    [RelayCommand]
    private void NavigateToSales()
    {
        _navigationService.NavigateTo<SalesViewModel>();
        CurrentView = _navigationService.CurrentView;
    }

    [RelayCommand]
    private void Logout()
    {
        IsLoggingOut = true;
        RequestClose?.Invoke();
    }
}
