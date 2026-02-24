using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    /// <summary>4e: Double-click a DataGrid row to open the edit form.</summary>
    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProductsViewModel vm
            && vm.SelectedProduct is not null
            && vm.ShowEditFormCommand.CanExecute(null))
        {
            vm.ShowEditFormCommand.Execute(null);
        }
    }
}
