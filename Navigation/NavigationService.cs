using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace StoreAssistantPro.Navigation;

public partial class NavigationService(IServiceProvider serviceProvider) : ObservableObject, INavigationService
{
    [ObservableProperty]
    public partial ObservableObject CurrentView { get; set; }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        CurrentView = serviceProvider.GetRequiredService<TViewModel>();
    }
}
