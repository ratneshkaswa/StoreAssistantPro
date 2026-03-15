using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Modules.Billing.Views;

public partial class BillingWindow : BaseDialogWindow
{
    private readonly IAppStateService _appState;
    private readonly IEventBus _eventBus;
    private OperationalMode _previousMode;
    private bool _modeTransitionActive;
    private bool _billingModeEvaluated;

    protected override double DialogWidth => 1200;
    protected override double DialogHeight => 850;
    protected override double DialogMinWidth => 960;
    protected override double DialogMinHeight => 720;
    protected override bool AllowResize => true;

    public BillingWindow(
        IWindowSizingService sizingService,
        IAppStateService appState,
        IEventBus eventBus,
        BillingViewModel vm) : base(sizingService)
    {
        _appState = appState;
        _eventBus = eventBus;
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) =>
        {
            ExitBillingMode();
            vm.Dispose();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnterBillingMode();
    }

    private void OnSearchResultsDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not BillingViewModel vm
            || sender is not DataGrid grid
            || grid.SelectedItem is not Product product)
        {
            return;
        }

        if (vm.AddProductToCartCommand.CanExecute(product))
            vm.AddProductToCartCommand.Execute(product);
    }

    private void OnSearchResultsKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter
            || DataContext is not BillingViewModel vm
            || sender is not DataGrid grid
            || grid.SelectedItem is not Product product)
        {
            return;
        }

        if (vm.AddProductToCartCommand.CanExecute(product))
        {
            vm.AddProductToCartCommand.Execute(product);
            e.Handled = true;
        }
    }

    private void EnterBillingMode()
    {
        if (_billingModeEvaluated)
            return;

        _billingModeEvaluated = true;
        _previousMode = _appState.CurrentMode;
        if (_previousMode == OperationalMode.Billing)
            return;

        _modeTransitionActive = true;
        _appState.SetMode(OperationalMode.Billing);
        _ = _eventBus.PublishAsync(new OperationalModeChangedEvent(_previousMode, OperationalMode.Billing));
    }

    private void ExitBillingMode()
    {
        if (!_modeTransitionActive)
            return;

        _modeTransitionActive = false;
        var currentMode = _appState.CurrentMode;
        if (currentMode == _previousMode)
            return;

        _appState.SetMode(_previousMode);
        _ = _eventBus.PublishAsync(new OperationalModeChangedEvent(currentMode, _previousMode));
    }
}
