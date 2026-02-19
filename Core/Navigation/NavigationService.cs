using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace StoreAssistantPro.Core.Navigation;

public partial class NavigationService(IServiceProvider serviceProvider) : ObservableObject, INavigationService
{
    private readonly Dictionary<string, Type> _pageMap = [];

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

        CurrentView = (ObservableObject)serviceProvider.GetRequiredService(vmType);
    }

    public void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject
    {
        _pageMap[pageKey] = typeof(TViewModel);
    }
}
