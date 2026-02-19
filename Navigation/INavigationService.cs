using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Navigation;

public interface INavigationService
{
    ObservableObject CurrentView { get; }
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}
