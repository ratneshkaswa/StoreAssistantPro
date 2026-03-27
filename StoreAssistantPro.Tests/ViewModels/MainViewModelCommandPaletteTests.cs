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
public sealed class MainViewModelCommandPaletteTests : IDisposable
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IStatusBarService _statusBar = Substitute.For<IStatusBarService>();
    private readonly IFeatureToggleService _features = Substitute.For<IFeatureToggleService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();

    public MainViewModelCommandPaletteTests()
    {
        UserPreferencesStore.ClearForTests();
        _appState.FirmName.Returns("Contoso Fabrics");
        _appState.Notifications.Returns(new ObservableCollection<AppNotification>());
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.CurrentUserType.Returns(UserType.Admin);
        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(DashboardSummary.Empty);
        _commandBus.SendAsync(Arg.Any<ICommandRequest<Unit>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult.Success()));
        _features.IsEnabled(Arg.Any<string>()).Returns(true);
        _userService.HasUserRoleAsync(Arg.Any<CancellationToken>()).Returns(true);
    }

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public void ToggleCommandPalette_Should_Populate_Items_And_Select_First_Result()
    {
        var sut = CreateSut();

        sut.ToggleCommandPaletteCommand.Execute(null);

        Assert.True(sut.IsCommandPaletteVisible);
        Assert.NotEmpty(sut.CommandPaletteItems);
        Assert.NotNull(sut.SelectedCommandPaletteItem);
        Assert.DoesNotContain(sut.CommandPaletteItems, item => item.Title == "Command Palette");
    }

    [Fact]
    public void CommandPaletteQuery_Should_Filter_Matching_Actions()
    {
        var sut = CreateSut();
        sut.ToggleCommandPaletteCommand.Execute(null);

        sut.CommandPaletteQuery = "vendor";

        Assert.NotEmpty(sut.CommandPaletteItems);
        Assert.Contains(sut.CommandPaletteItems, item => item.Title == "Vendors");
        Assert.DoesNotContain(sut.CommandPaletteItems, item => item.Title == "Billing");
    }

    [Fact]
    public void ExecuteSelectedCommandPaletteItem_Should_Close_And_Record_Recent_Action()
    {
        var sut = CreateSut();
        sut.ToggleCommandPaletteCommand.Execute(null);
        sut.CommandPaletteQuery = "reports";

        var reportsItem = Assert.Single(sut.CommandPaletteItems);
        Assert.Equal("Reports", reportsItem.Title);

        sut.ExecuteSelectedCommandPaletteItemCommand.Execute(null);

        Assert.False(sut.IsCommandPaletteVisible);
        Assert.Equal(
            "Reports \u2014 Contoso Fabrics \u2014 Store Assistant Pro",
            sut.WindowTitle);

        sut.ToggleCommandPaletteCommand.Execute(null);

        var firstResult = sut.CommandPaletteItems.First();
        Assert.Equal("Reports", firstResult.Title);
        Assert.True(firstResult.IsRecent);
    }

    [Fact]
    public void GetShortcutEntries_Should_Include_CommandPalette_Without_Duplicates()
    {
        var sut = CreateSut();

        var entries = sut.GetShortcutEntries();

        var paletteEntries = entries
            .Where(entry => entry.Key == "Ctrl+K" && entry.Title == "Command Palette")
            .ToList();

        Assert.Single(paletteEntries);
        Assert.Single(entries, entry => entry.Key == "F1" && entry.Title == "Shortcuts");
        Assert.Single(entries, entry => entry.Key == "Ctrl+F" && entry.Title == "Search");
    }

    [Fact]
    public void QuickActionOverflow_Should_Move_Extra_Actions_Into_Flyout_When_View_Is_Narrow()
    {
        var sut = CreateSut();

        sut.QuickActionBarViewportWidth = 220;

        Assert.True(sut.HasOverflowQuickActions);
        Assert.NotEmpty(sut.OverflowQuickActions);
        Assert.True(sut.VisibleQuickActions.Count < sut.QuickAccessActions.Count);
    }

    [Fact]
    public void QuickActionOverflow_Should_Clear_When_View_Is_Wide_Enough()
    {
        var sut = CreateSut();

        sut.QuickActionBarViewportWidth = 220;
        sut.QuickActionBarViewportWidth = 4096;

        Assert.False(sut.HasOverflowQuickActions);
        Assert.Empty(sut.OverflowQuickActions);
        Assert.Equal(sut.QuickAccessActions.Count, sut.VisibleQuickActions.Count);
    }

    [Fact]
    public void QuickAccessBar_Should_Show_Global_Utilities_Without_Duplicating_Rail_Navigation()
    {
        var sut = CreateSut();

        Assert.Contains(sut.QuickActions, action => action.Title == "Reports");
        Assert.Contains(sut.QuickActions, action => action.Title == "Billing");
        Assert.DoesNotContain(sut.QuickActions, action => action.Title == "Refresh");
        Assert.DoesNotContain(sut.QuickActions, action => action.Title == "Logout");

        Assert.Contains(sut.QuickAccessActions, action => action.Title == "Refresh");
        Assert.Contains(sut.QuickAccessActions, action => action.Title == "Search");
        Assert.Contains(sut.QuickAccessActions, action => action.Title == "Command Palette");
        Assert.Contains(sut.QuickAccessActions, action => action.Title == "Shortcuts");
        Assert.Contains(sut.QuickAccessActions, action => action.Title == "Logout");
        Assert.DoesNotContain(sut.QuickAccessActions, action => action.Title == "Reports");
        Assert.DoesNotContain(sut.QuickAccessActions, action => action.Title == "Billing");
    }

    [Fact]
    public void Navigating_To_A_Page_Should_Mark_Its_QuickAction_As_Active()
    {
        var sut = CreateSut();

        sut.OpenReportsCommand.Execute(null);

        Assert.True(sut.QuickActions.Single(action => action.Title == "Reports").IsActive);
        Assert.False(sut.QuickActions.Single(action => action.Title == "Home").IsActive);
        Assert.False(sut.QuickActions.Single(action => action.Title == "Billing").IsActive);
    }

    [Fact]
    public void NavigationRail_Toggle_Should_Expand_On_Home_And_Go_Back_From_Inner_Page()
    {
        var sut = CreateSut();

        sut.NavigateToMainWorkspaceCommand.Execute(null);

        Assert.False(sut.IsNavigationRailBackMode);

        sut.ToggleNavigationRailOrNavigateBackCommand.Execute(null);

        Assert.True(sut.IsNavigationRailExpanded);
        Assert.Equal(320, sut.NavigationRailWidth);

        sut.OpenReportsCommand.Execute(null);

        Assert.True(sut.IsNavigationRailBackMode);

        sut.ToggleNavigationRailOrNavigateBackCommand.Execute(null);

        Assert.False(sut.IsNavigationRailBackMode);
        Assert.Contains("Home", sut.WindowTitle);
    }

    private MainViewModel CreateSut()
    {
        var navigation = new StubNavigationService();
        return new MainViewModel(
            navigation,
            Substitute.For<ISessionService>(),
            _dialogService,
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
        public void CachePage(string pageKey) { }
        public void InvalidatePageCache(string pageKey) { }
        public void MapFeature(string pageKey, string featureFlag) { }
    }

    private sealed class StubView : ObservableObject;
}
