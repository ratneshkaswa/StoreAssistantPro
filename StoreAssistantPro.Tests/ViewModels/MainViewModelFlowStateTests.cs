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
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IStatusBarService _statusBar = Substitute.For<IStatusBarService>();
    private readonly IFeatureToggleService _features = Substitute.For<IFeatureToggleService>();

    public MainViewModelFlowStateTests()
    {
        _appState.FirmName.Returns("Contoso Fabrics");
        _appState.Notifications.Returns(new ObservableCollection<AppNotification>());
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.CurrentUserType.Returns(UserType.Admin);
        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(DashboardSummary.Empty);
        _commandBus.SendAsync(Arg.Any<ICommandRequest<Unit>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult.Success()));
    }

    private MainViewModel CreateSut()
    {
        var nav = new StubNavigationService();
        return new MainViewModel(
            nav,
            Substitute.For<ISessionService>(),
            _dialogService,
            _appState,
            Substitute.For<IWorkflowManager>(),
            _commandBus,
            _eventBus,
            _features,
            _statusBar,
            Substitute.For<IQuickActionService>(),
            Substitute.For<IShortcutService>(),
            _dashboardService,
            Substitute.For<INotificationService>(),
            Substitute.For<IToastService>(),
            Substitute.For<IRegionalSettingsService>(),
            []);
    }

    [Fact]
    public void CreateSut_DoesNotThrow()
    {
        var sut = CreateSut();
        Assert.NotNull(sut);
    }

    [Fact]
    public void OpenVendorManagement_NavigatesToVendorPage()
    {
        var sut = CreateSut();

        sut.OpenVendorManagementCommand.Execute(null);

        _statusBar.Received(1).SetPersistent("Vendor management");
        Assert.Equal(
            "Vendor management — Contoso Fabrics — Store Assistant Pro",
            sut.WindowTitle);
    }

    [Fact]
    public void NavigateToMainWorkspace_UpdatesWindowTitleToHome()
    {
        var sut = CreateSut();

        sut.NavigateToMainWorkspaceCommand.Execute(null);

        Assert.Equal(
            "Home — Contoso Fabrics — Store Assistant Pro",
            sut.WindowTitle);
    }

    [Fact]
    public void Dispose_ClearsRequestClose()
    {
        var sut = CreateSut();
        sut.RequestClose = () => { };

        sut.Dispose();

        Assert.Null(sut.RequestClose);
    }

    // ── Stub for INavigationService (must be ObservableObject) ───

    private sealed class StubNavigationService : ObservableObject, INavigationService
    {
        private ObservableObject _currentView = new StubView();
        private string? _currentPageKey;

        public ObservableObject CurrentView
        {
            get => _currentView;
            private set => SetProperty(ref _currentView, value);
        }

        public string? CurrentPageKey
        {
            get => _currentPageKey;
            private set => SetProperty(ref _currentPageKey, value);
        }

        public void NavigateTo<TViewModel>() where TViewModel : ObservableObject { }
        public void NavigateTo(string pageKey)
        {
            CurrentPageKey = pageKey;
            CurrentView = new StubView();
        }
        public void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject { }
        public void MapFeature(string pageKey, string featureFlag) { }
    }

    private sealed class StubView : ObservableObject;
}
