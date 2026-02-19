using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core.Navigation;

public interface INavigationService
{
    ObservableObject CurrentView { get; }
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    void NavigateTo(string pageKey);
    void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject;
}
