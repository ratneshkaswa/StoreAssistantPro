using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Sales.ViewModels;

namespace StoreAssistantPro.Modules.Sales.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalesViewModel vm)
        {
            await vm.LoadSalesCommand.ExecuteAsync(null);
        }
    }
}
