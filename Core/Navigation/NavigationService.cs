using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;

namespace StoreAssistantPro.Core.Navigation;

public partial class NavigationService(
    IServiceProvider serviceProvider,
    IFeatureToggleService featureToggle) : ObservableObject, INavigationService
{
    private readonly Dictionary<string, Type> _pageMap = [];
    private readonly Dictionary<string, string> _featureMap = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    public partial ObservableObject CurrentView { get; set; }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        CurrentView = serviceProvider.GetRequiredService<TViewModel>();
    }

    public void NavigateTo(string pageKey)
    {
        if (!_pageMap.TryGetValue(pageKey, out var vmType))
            throw new InvalidOperationException($"No ViewModel registered for page key '{pageKey}'.");

        // Block navigation when the feature is disabled for the current mode
        if (_featureMap.TryGetValue(pageKey, out var flag) && !featureToggle.IsEnabled(flag))
            return;

        CurrentView = (ObservableObject)serviceProvider.GetRequiredService(vmType);
    }

    public void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject
    {
        _pageMap[pageKey] = typeof(TViewModel);
    }

    public void MapFeature(string pageKey, string featureFlag)
    {
        _featureMap[pageKey] = featureFlag;
    }
}
