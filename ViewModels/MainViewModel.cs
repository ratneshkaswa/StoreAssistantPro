using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Services;

namespace StoreAssistantPro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial ObservableObject CurrentView { get; set; }

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
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
}
