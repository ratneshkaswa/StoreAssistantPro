using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Features;

namespace StoreAssistantPro.Core.Navigation;

public partial class NavigationService(
    IServiceProvider serviceProvider,
    IFeatureToggleService featureToggle,
    ILogger<NavigationService> logger) : ObservableObject, INavigationService
{
    private readonly Dictionary<string, Type> _pageMap = [];
    private readonly Dictionary<string, string> _featureMap = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    public partial ObservableObject CurrentView { get; set; }

    [ObservableProperty]
    public partial string? CurrentPageKey { get; set; }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        ActivateView(serviceProvider.GetRequiredService<TViewModel>(), typeof(TViewModel).Name);
    }

    public void NavigateTo(string pageKey)
    {
        if (!_pageMap.TryGetValue(pageKey, out var vmType))
            throw new InvalidOperationException($"No ViewModel registered for page key '{pageKey}'.");

        // Block navigation when the feature is disabled for the current mode
        if (_featureMap.TryGetValue(pageKey, out var flag) && !featureToggle.IsEnabled(flag))
            return;

        var newView = (ObservableObject)serviceProvider.GetRequiredService(vmType);
        CurrentPageKey = pageKey;
        ActivateView(newView, pageKey);
    }

    public void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject
    {
        _pageMap[pageKey] = typeof(TViewModel);
    }

    public void MapFeature(string pageKey, string featureFlag)
    {
        _featureMap[pageKey] = featureFlag;
    }

    // ── Lifecycle orchestration ──────────────────────────────────────

    private void ActivateView(ObservableObject newView, string label)
    {
        var previous = CurrentView;

        // Notify outgoing VM and dispose it
        if (previous is not null && !ReferenceEquals(previous, newView))
        {
            if (previous is INavigationAware outgoing)
                outgoing.OnNavigatedFrom();

            if (previous is IDisposable disposable)
            {
                disposable.Dispose();
                logger.LogDebug("Disposed outgoing view: {ViewType}", previous.GetType().Name);
            }
        }

        CurrentView = newView;

        // Notify incoming VM
        if (newView is INavigationAware incoming)
        {
            _ = NotifyNavigatedToAsync(incoming, label);
        }
    }

    private async Task NotifyNavigatedToAsync(INavigationAware aware, string label)
    {
        try
        {
            await aware.OnNavigatedTo();
        }
        catch (OperationCanceledException)
        {
            // Navigated away before OnNavigatedTo completed — expected.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OnNavigatedTo failed for {Page}", label);
        }
    }
}
