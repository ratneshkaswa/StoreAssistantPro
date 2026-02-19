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

    public void ApplyTo(INavigationService navigationService)
    {
        foreach (var registration in _registrations)
            registration(navigationService);
    }
}
