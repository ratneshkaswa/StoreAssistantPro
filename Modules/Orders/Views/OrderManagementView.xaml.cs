using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Orders.ViewModels;

namespace StoreAssistantPro.Modules.Orders.Views;

public partial class OrderManagementView : UserControl
{
    public OrderManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is OrderManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
