using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Products.ViewModels;

namespace StoreAssistantPro.Modules.Products.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProductsViewModel vm)
        {
            await vm.LoadProductsCommand.ExecuteAsync(null);
        }
    }
}
