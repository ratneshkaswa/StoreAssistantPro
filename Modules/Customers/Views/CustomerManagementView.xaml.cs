using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Customers.ViewModels;

namespace StoreAssistantPro.Modules.Customers.Views;

public partial class CustomerManagementView : UserControl
{
    public CustomerManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomerManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
