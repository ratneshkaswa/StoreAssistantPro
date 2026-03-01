using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Sales.ViewModels;

namespace StoreAssistantPro.Modules.Sales.Views;

public partial class SaleReturnsView : UserControl
{
    public SaleReturnsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SaleReturnsViewModel vm)
            await vm.LoadReturnsCommand.ExecuteAsync(null);
    }
}
