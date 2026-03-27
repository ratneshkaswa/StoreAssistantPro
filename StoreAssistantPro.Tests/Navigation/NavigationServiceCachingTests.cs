using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Navigation;

public sealed class NavigationServiceCachingTests
{
    [Fact]
    public void CachePage_Should_Reuse_ViewModel_Instance_On_Revisit()
    {
        using var provider = BuildServices(services => services.AddTransient<CachedVm>());
        var sut = CreateSut(provider);
        sut.RegisterPage<CachedVm>("Cached");
        sut.CachePage("Cached");

        sut.NavigateTo("Cached");
        var first = Assert.IsType<CachedVm>(sut.CurrentView);

        sut.NavigateTo("Cached");
        var second = Assert.IsType<CachedVm>(sut.CurrentView);

        Assert.Same(first, second);
        Assert.Equal(2, second.NavigatedToCount);
    }

    [Fact]
    public void InvalidatePageCache_Should_Force_New_Instance_On_Next_Navigation()
    {
        using var provider = BuildServices(services => services.AddTransient<CachedVm>());
        var sut = CreateSut(provider);
        sut.RegisterPage<CachedVm>("Cached");
        sut.CachePage("Cached");

        sut.NavigateTo("Cached");
        var first = Assert.IsType<CachedVm>(sut.CurrentView);

        sut.InvalidatePageCache("Cached");
        sut.NavigateTo("Cached");
        var second = Assert.IsType<CachedVm>(sut.CurrentView);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void NonCached_Page_Should_Create_New_Instance_On_Each_Visit()
    {
        using var provider = BuildServices(services => services.AddTransient<TransientVm>());
        var sut = CreateSut(provider);
        sut.RegisterPage<TransientVm>("Transient");

        sut.NavigateTo("Transient");
        var first = Assert.IsType<TransientVm>(sut.CurrentView);

        sut.NavigateTo("Transient");
        var second = Assert.IsType<TransientVm>(sut.CurrentView);

        Assert.NotSame(first, second);
    }

    private static ServiceProvider BuildServices(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static NavigationService CreateSut(IServiceProvider serviceProvider)
    {
        var features = Substitute.For<IFeatureToggleService>();
        features.IsEnabled(Arg.Any<string>()).Returns(true);
        var perf = new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
        return new NavigationService(
            serviceProvider,
            features,
            perf,
            NullLogger<NavigationService>.Instance);
    }

    private sealed partial class CachedVm : ObservableObject, INavigationAware
    {
        public int NavigatedToCount { get; private set; }

        public Task OnNavigatedTo(CancellationToken ct = default)
        {
            NavigatedToCount++;
            return Task.CompletedTask;
        }

        public void OnNavigatedFrom()
        {
        }
    }

    private sealed partial class TransientVm : ObservableObject;
}
