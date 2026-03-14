using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Modules.Billing.Views;

public partial class BillingWindow : BaseDialogWindow
{
    protected override double DialogWidth => 1200;
    protected override double DialogHeight => 850;
    protected override double DialogMinWidth => 960;
    protected override double DialogMinHeight => 720;
    protected override bool AllowResize => true;

    public BillingWindow(
        IWindowSizingService sizingService,
        BillingViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Billing window starts with empty cart, no async load needed
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
}
