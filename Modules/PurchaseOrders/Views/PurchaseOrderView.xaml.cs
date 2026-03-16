using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.PurchaseOrders.ViewModels;

namespace StoreAssistantPro.Modules.PurchaseOrders.Views;

public partial class PurchaseOrderView : UserControl
{
    public PurchaseOrderView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PurchaseOrderViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
