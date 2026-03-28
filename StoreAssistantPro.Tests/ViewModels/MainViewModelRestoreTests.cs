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
using StoreAssistantPro.Modules.Users.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class MainViewModelRestoreTests : IDisposable
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly IFeatureToggleService _features = Substitute.For<IFeatureToggleService>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly IStatusBarService _statusBar = Substitute.For<IStatusBarService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();

    public MainViewModelRestoreTests()
    {
        UserPreferencesStore.ClearForTests();
        AppLaunchActivationStore.ResetForTests();
        _appState.FirmName.Returns("Contoso Fabrics");
        _appState.Notifications.Returns(new ObservableCollection<AppNotification>());
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.CurrentUserType.Returns(UserType.Admin);
        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(DashboardSummary.Empty);
        _commandBus.SendAsync(Arg.Any<ICommandRequest<Unit>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult.Success()));
        _sessionService.LoginAsync(Arg.Any<UserType>()).Returns(Task.CompletedTask);
        _features.IsEnabled(Arg.Any<string>()).Returns(true);
        _userService.HasUserRoleAsync(Arg.Any<CancellationToken>()).Returns(true);
    }

    public void Dispose()
    {
        UserPreferencesStore.ClearForTests();
        AppLaunchActivationStore.ResetForTests();
    }

    [Fact]
    public void Constructor_Should_Restore_Recent_CommandPalette_Items()
    {
        UserPreferencesStore.Update(state =>
        {
            state.RecentCommandPaletteItemIds = ["Reports", "Billing"];
        });

        var sut = CreateSut();

        sut.ToggleCommandPaletteCommand.Execute(null);
        Assert.Contains(sut.CommandPaletteItems, item => item.Title == "Reports" && item.IsRecent);
        Assert.Contains(sut.CommandPaletteItems, item => item.Title == "Billing" && item.IsRecent);
    }

    [Fact]
    public void Constructor_Should_Not_Navigate_To_Login_Before_AutoLogin_Resolves()
    {
        var sut = CreateSut();

        Assert.DoesNotContain("Sign in", sut.WindowTitle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_Should_Restore_Last_Visited_Page_And_Consume_Notification_Activation()
    {
        UserPreferencesStore.Update(state =>
        {
            state.RestoreLastVisitedPageOnLogin = true;
            state.LastVisitedPage = "Reports";
        });
        AppLaunchActivationStore.Initialize(["open-notifications"]);
        var sut = CreateSut();

        await InvokeLoginSucceededAsync(sut, UserType.User);

        Assert.Contains("Reports", sut.WindowTitle);
        Assert.True(sut.IsNotificationsPanelVisible);
    }

    [Fact]
    public async Task Login_Should_Honor_Activation_Page_From_Windows_Notification()
    {
        AppLaunchActivationStore.Initialize(
        [
            "action=open-notifications;notificationId=1AA54AF1-0B1D-4A38-B0F0-8F5DA788A4A1;pageKey=SystemSettings"
        ]);
        var sut = CreateSut();

        await InvokeLoginSucceededAsync(sut, UserType.Admin);

        Assert.Contains("System settings", sut.WindowTitle, StringComparison.OrdinalIgnoreCase);
        Assert.True(sut.IsNotificationsPanelVisible);
    }

    [Fact]
    public async Task Login_When_InitialSetupPending_Should_Route_To_Firm_Setup()
    {
        _appState.IsInitialSetupPending.Returns(true);
        var sut = CreateSut();

        await InvokeLoginSucceededAsync(sut, UserType.Admin);

        Assert.Contains("Firm", sut.WindowTitle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PrepareStartup_When_NoUserRoleConfigured_Should_Show_Login()
    {
        _userService.HasUserRoleAsync(Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut();

        await InvokePrepareStartupAsync(sut);

        Assert.Contains("Sign in", sut.WindowTitle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShellChrome_Should_Remain_Hidden_On_Login_Page_Even_When_Session_Is_Already_Marked_LoggedIn()
    {
        _appState.IsLoggedIn.Returns(true);
        _userService.HasUserRoleAsync(Arg.Any<CancellationToken>()).Returns(false);
        var sut = CreateSut();

        await InvokePrepareStartupAsync(sut);

        Assert.False(sut.IsShellChromeVisible);

        await InvokeLoginSucceededAsync(sut, UserType.Admin);

        Assert.True(sut.IsShellChromeVisible);
    }

    [Fact]
    public async Task ShellChrome_Should_Wait_For_View_Swap_Before_Leaving_Login_State()
    {
        _appState.IsLoggedIn.Returns(true);
        _userService.HasUserRoleAsync(Arg.Any<CancellationToken>()).Returns(false);
        var navigation = new StubNavigationService();
        var sut = CreateSut(navigation);

        await InvokePrepareStartupAsync(sut);

        navigation.PublishPageKeyOnly("FirmManagement");
        Assert.False(sut.IsShellChromeVisible);
        Assert.Contains("Sign in", sut.WindowTitle, StringComparison.OrdinalIgnoreCase);

        navigation.PublishViewOnly();
        Assert.True(sut.IsShellChromeVisible);
        Assert.Contains("Firm", sut.WindowTitle, StringComparison.OrdinalIgnoreCase);
    }

    private MainViewModel CreateSut()
    {
        return CreateSut(new StubNavigationService());
    }

    private MainViewModel CreateSut(StubNavigationService navigation)
    {
        return new MainViewModel(
            navigation,
            _sessionService,
            Substitute.For<IDialogService>(),
            _appState,
            Substitute.For<IWorkflowManager>(),
            _commandBus,
            _eventBus,
            _features,
            _statusBar,
            new QuickActionService(),
            Substitute.For<IShortcutService>(),
            _dashboardService,
            Substitute.For<INotificationService>(),
            Substitute.For<IToastService>(),
            Substitute.For<IRegionalSettingsService>(),
            _userService,
            []);
    }

    private static Task InvokeLoginSucceededAsync(MainViewModel viewModel, UserType userType)
    {
        var method = typeof(MainViewModel).GetMethod("OnLoginSucceededAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate login success handler.");
        return (Task)(method.Invoke(viewModel, [userType]) ?? Task.CompletedTask);
    }

    private static Task InvokePrepareStartupAsync(MainViewModel viewModel)
    {
        var method = typeof(MainViewModel).GetMethod("PrepareStartupAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate startup preparation method.");
        return (Task)(method.Invoke(viewModel, null) ?? Task.CompletedTask);
    }

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

        public void PublishPageKeyOnly(string pageKey)
        {
            CurrentPageKey = pageKey;
        }

        public void PublishViewOnly()
        {
            CurrentView = new StubView();
        }

        public void RegisterPage<TViewModel>(string pageKey) where TViewModel : ObservableObject { }
        public void CachePage(string pageKey) { }
        public void InvalidatePageCache(string pageKey) { }
        public void MapFeature(string pageKey, string featureFlag) { }
    }

    private sealed class StubView : ObservableObject;
}
