using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.SalesPurchase.ViewModels;

namespace StoreAssistantPro.Modules.SalesPurchase.Views;

public partial class SalesPurchaseView : UserControl
{
    public SalesPurchaseView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalesPurchaseViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
