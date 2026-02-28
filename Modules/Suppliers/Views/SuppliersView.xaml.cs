using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Suppliers.ViewModels;

namespace StoreAssistantPro.Modules.Suppliers.Views;

public partial class SuppliersView : UserControl
{
    public SuppliersView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SuppliersViewModel vm)
        {
            await vm.LoadSuppliersCommand.ExecuteAsync(null);
        }
    }
}
