using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Services;

public interface INavigationService
{
    ObservableObject CurrentView { get; }
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}
