using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Navigation;

public partial class NavigationService(
    IServiceProvider serviceProvider,
    IFeatureToggleService featureToggle,
    IPerformanceMonitor performanceMonitor,
    ILogger<NavigationService> logger) : ObservableObject, INavigationService
{
    private readonly Dictionary<string, Type> _pageMap = [];
    private readonly Dictionary<string, string> _featureMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _cacheablePages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ObservableObject> _cachedViews = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    public partial ObservableObject CurrentView { get; set; }

    [ObservableProperty]
    public partial string? CurrentPageKey { get; set; }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        var label = typeof(TViewModel).Name;
        var previousPageKey = CurrentPageKey;
        ActivateView(serviceProvider.GetRequiredService<TViewModel>(), label, previousPageKey);
    }

    public void NavigateTo(string pageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);

        if (!_pageMap.TryGetValue(pageKey, out var vmType))
            throw new InvalidOperationException($"No ViewModel registered for page key '{pageKey}'.");

        // Block navigation when the feature is disabled for the current mode
        if (_featureMap.TryGetValue(pageKey, out var flag) && !featureToggle.IsEnabled(flag))
            return;

        using var _ = performanceMonitor.BeginScope($"NavigationService.NavigateTo[{pageKey}]", TimeSpan.FromMilliseconds(150));

        var previousPageKey = CurrentPageKey;
        var newView = ResolveView(pageKey, vmType);
        ActivateView(newView, pageKey, previousPageKey);
    }

    public void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject
    {
        _pageMap[pageKey] = typeof(TViewModel);
    }

    public void CachePage(string pageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);
        _cacheablePages.Add(pageKey);
    }

    public void InvalidatePageCache(string pageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);
        _cachedViews.Remove(pageKey);
    }

    public void MapFeature(string pageKey, string featureFlag)
    {
        _featureMap[pageKey] = featureFlag;
    }

    // ── Lifecycle orchestration ──────────────────────────────────────

    private ObservableObject ResolveView(string pageKey, Type vmType)
    {
        if (_cacheablePages.Contains(pageKey) && _cachedViews.TryGetValue(pageKey, out var cached))
        {
            logger.LogDebug("Reusing cached view model for {Page} ({ViewType})", pageKey, cached.GetType().Name);
            return cached;
        }

        var created = (ObservableObject)serviceProvider.GetRequiredService(vmType);
        if (_cacheablePages.Contains(pageKey))
            _cachedViews[pageKey] = created;

        return created;
    }

    private void ActivateView(ObservableObject newView, string label, string? previousPageKey)
    {
        var previous = CurrentView;

        // Notify outgoing VM and dispose it
        if (previous is not null && !ReferenceEquals(previous, newView))
        {
            if (previous is INavigationAware outgoing)
                outgoing.OnNavigatedFrom();

            if (ShouldDispose(previous, previousPageKey) && previous is IDisposable disposable)
            {
                disposable.Dispose();
                logger.LogDebug("Disposed outgoing view: {ViewType}", previous.GetType().Name);
            }
        }

        CurrentPageKey = label;
        CurrentView = newView;
        logger.LogInformation("Navigated to {Page} ({ViewType})", label, newView.GetType().Name);

        // Notify incoming VM
        if (newView is INavigationAware incoming)
        {
            _ = NotifyNavigatedToAsync(incoming, label);
        }
    }

    private bool ShouldDispose(ObservableObject previous, string? previousPageKey)
    {
        if (string.IsNullOrWhiteSpace(previousPageKey))
            return true;

        if (!_cacheablePages.Contains(previousPageKey))
            return true;

        return !_cachedViews.TryGetValue(previousPageKey, out var cached)
            || !ReferenceEquals(cached, previous);
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
