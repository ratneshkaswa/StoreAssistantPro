using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Customers.ViewModels;

namespace StoreAssistantPro.Modules.Customers.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomersViewModel vm)
            await vm.LoadCustomersCommand.ExecuteAsync(null);
    }
}
