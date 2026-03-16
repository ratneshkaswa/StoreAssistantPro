using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Products.ViewModels;

namespace StoreAssistantPro.Modules.Products.Views;

public partial class ProductManagementView : UserControl
{
    public ProductManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductManagementViewModel vm)
            return;

        try { await vm.LoadCommand.ExecuteAsync(null); }
        catch { /* RunLoadAsync handles logging */ }
    }
}
