using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Modules.Billing.Views;

public partial class SaleHistoryView : UserControl
{
    public SaleHistoryView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SaleHistoryViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
