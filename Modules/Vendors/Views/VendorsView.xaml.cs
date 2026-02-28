using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Vendors.ViewModels;

namespace StoreAssistantPro.Modules.Vendors.Views;

public partial class VendorsView : UserControl
{
    public VendorsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is VendorsViewModel vm)
        {
            await vm.LoadVendorsCommand.ExecuteAsync(null);
        }
    }
}
