using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Sales.ViewModels;

namespace StoreAssistantPro.Modules.Sales.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
    }

    /// <summary>Route column-header sort clicks to ViewModel server-side sort.</summary>
    private void OnSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        if (DataContext is SalesViewModel vm
            && e.Column.SortMemberPath is { Length: > 0 } sortPath
            && vm.SortByColumnCommand.CanExecute(sortPath))
        {
            vm.SortByColumnCommand.Execute(sortPath);
            SyncSortIndicators(sender as DataGrid, vm);
        }
    }

    private static void SyncSortIndicators(DataGrid? grid, SalesViewModel vm)
    {
        if (grid is null) return;

        foreach (var col in grid.Columns)
        {
            if (string.Equals(col.SortMemberPath, vm.SortColumn, StringComparison.OrdinalIgnoreCase))
            {
                col.SortDirection = vm.SortDescending
                    ? System.ComponentModel.ListSortDirection.Descending
                    : System.ComponentModel.ListSortDirection.Ascending;
            }
            else
            {
                col.SortDirection = null;
            }
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalesViewModel vm)
        {
            await vm.LoadSalesCommand.ExecuteAsync(null);
        }
    }
}
