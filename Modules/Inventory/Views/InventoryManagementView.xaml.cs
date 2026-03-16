using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Inventory.ViewModels;

namespace StoreAssistantPro.Modules.Inventory.Views;

public partial class InventoryManagementView : UserControl
{
    public InventoryManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InventoryViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
