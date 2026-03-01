using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Inventory.ViewModels;

namespace StoreAssistantPro.Modules.Inventory.Views;

public partial class StockAlertsView : UserControl
{
    public StockAlertsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StockAlertsViewModel vm)
            await vm.LoadAlertsCommand.ExecuteAsync(null);
    }
}
