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

    /// <summary>Sync DataGrid multi-select to ViewModel for bulk operations.</summary>
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProductsViewModel vm && sender is DataGrid grid)
        {
            vm.UpdateSelectedProducts(grid.SelectedItems);
        }
    }

    /// <summary>Route column-header sort clicks to ViewModel server-side sort.</summary>
    private void OnSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;

        if (DataContext is ProductsViewModel vm
            && e.Column.SortMemberPath is { Length: > 0 } sortPath
            && vm.SortByColumnCommand.CanExecute(sortPath))
        {
            vm.SortByColumnCommand.Execute(sortPath);

            e.Column.SortDirection = vm.SortDescending
                ? System.ComponentModel.ListSortDirection.Descending
                : System.ComponentModel.ListSortDirection.Ascending;
        }
    }
}
