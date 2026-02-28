using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core.Navigation;

public interface INavigationService : INotifyPropertyChanged
{
    ObservableObject CurrentView { get; }
    string? CurrentPageKey { get; }
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    void NavigateTo(string pageKey);
    void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject;

    /// <summary>
    /// Associates a page key with a feature flag so <see cref="NavigateTo(string)"/>
    /// can reject navigation when the feature is disabled.
    /// </summary>
    void MapFeature(string pageKey, string featureFlag);
}
