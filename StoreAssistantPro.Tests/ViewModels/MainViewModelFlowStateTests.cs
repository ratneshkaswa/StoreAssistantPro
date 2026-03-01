using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class MainViewModelFlowStateTests
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();

    public MainViewModelFlowStateTests()
    {
        _appState.Notifications.Returns(new ObservableCollection<AppNotification>());
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _dashboardService.GetSummaryAsync().Returns(DashboardSummary.Empty);
    }

    private MainViewModel CreateSut()
    {
        var nav = new StubNavigationService();
        return new MainViewModel(
            nav,
            Substitute.For<ISessionService>(),
            Substitute.For<IDialogService>(),
            _appState,
            Substitute.For<IWorkflowManager>(),
            Substitute.For<ICommandBus>(),
            _eventBus,
            Substitute.For<IFeatureToggleService>(),
            Substitute.For<IStatusBarService>(),
            Substitute.For<IQuickActionService>(),
            Substitute.For<IShortcutService>(),
            _dashboardService,
            Substitute.For<INotificationService>(),
            []);
    }

    [Fact]
    public void CreateSut_DoesNotThrow()
    {
        var sut = CreateSut();
        Assert.NotNull(sut);
    }

    // ── Stub for INavigationService (must be ObservableObject) ───

    private sealed class StubNavigationService : ObservableObject, INavigationService
    {
        public ObservableObject CurrentView { get; } = new StubView();
        public string CurrentPageKey { get; } = string.Empty;

        public void NavigateTo<TViewModel>() where TViewModel : ObservableObject { }
        public void NavigateTo(string pageKey) { }
        public void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject { }
        public void MapFeature(string pageKey, string featureFlag) { }
    }

    private sealed class StubView : ObservableObject;
}
