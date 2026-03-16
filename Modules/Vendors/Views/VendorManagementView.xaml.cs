using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Vendors.ViewModels;

namespace StoreAssistantPro.Modules.Vendors.Views;

public partial class VendorManagementView : UserControl
{
    public VendorManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is VendorManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
