using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core.Navigation;

/// <summary>
/// Collects page-key → ViewModel-type mappings during DI registration.
/// After the host is built, <see cref="ApplyTo"/> pushes all mappings
/// into the <see cref="INavigationService"/> so modules never reference
/// each other's ViewModel types at compile time.
/// </summary>
public class NavigationPageRegistry
{
    private readonly List<Action<INavigationService>> _registrations = [];

    public NavigationPageRegistry Map<TViewModel>(string pageKey) where TViewModel : ObservableObject
    {
        _registrations.Add(nav => nav.RegisterPage<TViewModel>(pageKey));
        return this;
    }

    public NavigationPageRegistry CachePage(string pageKey)
    {
        _registrations.Add(nav => nav.CachePage(pageKey));
        return this;
    }

    /// <summary>
    /// Maps a page key to a feature flag. Navigation to this page is
    /// blocked when <see cref="Core.Features.IFeatureToggleService.IsEnabled"/>
    /// returns <c>false</c> for the given flag.
    /// </summary>
    public NavigationPageRegistry RequireFeature(string pageKey, string featureFlag)
    {
        _registrations.Add(nav => nav.MapFeature(pageKey, featureFlag));
        return this;
    }

    public void ApplyTo(INavigationService navigationService)
    {
        foreach (var registration in _registrations)
            registration(navigationService);
    }
}
