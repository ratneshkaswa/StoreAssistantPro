using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Modules.Billing.Views;

public partial class BillingView : UserControl
{
    private IAppStateService? _appState;
    private IEventBus? _eventBus;
    private OperationalMode _previousMode;
    private bool _modeTransitionActive;
    private bool _billingModeEvaluated;

    public BillingView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var sp = ((App)Application.Current).Services!;
        _appState = sp.GetRequiredService<IAppStateService>();
        _eventBus = sp.GetRequiredService<IEventBus>();

        EnterBillingMode();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ExitBillingMode();
    }

    private void OnSearchResultsDoubleClick(object sender, MouseButtonEventArgs e)
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
        if (_billingModeEvaluated || _appState is null || _eventBus is null)
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
        if (!_modeTransitionActive || _appState is null || _eventBus is null)
            return;

        _modeTransitionActive = false;
        var currentMode = _appState.CurrentMode;
        if (currentMode == _previousMode)
            return;

        _appState.SetMode(_previousMode);
        _ = _eventBus.PublishAsync(new OperationalModeChangedEvent(currentMode, _previousMode));
    }
}
