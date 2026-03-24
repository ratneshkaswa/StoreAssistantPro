using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.ViewModels;

namespace StoreAssistantPro.Modules.Brands.Views;

public partial class BrandManagementView : UserControl
{
    private readonly Dictionary<int, string> _pendingBrandNames = [];

    public BrandManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BrandManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }

    private void OnBrandsGridPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        BeginInlineEdit((DataGrid)sender, e, BrandNameColumn);

    private void OnBrandsGridBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (ReferenceEquals(e.Column, BrandNameColumn) && e.Row.Item is Brand brand)
            _pendingBrandNames[brand.Id] = brand.Name;
    }

    private async void OnBrandsGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit
            || !ReferenceEquals(e.Column, BrandNameColumn)
            || e.Row.Item is not Brand brand
            || e.EditingElement is not TextBox textBox
            || DataContext is not BrandManagementViewModel vm)
        {
            return;
        }

        var originalName = _pendingBrandNames.TryGetValue(brand.Id, out var value)
            ? value
            : brand.Name;
        _pendingBrandNames.Remove(brand.Id);

        var success = await vm.TryInlineRenameAsync(brand, textBox.Text, originalName);
        if (!success)
            RefreshItemsView((DataGrid)sender);
    }

    private void RefreshItemsView(ItemsControl itemsControl)
    {
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => CollectionViewSource.GetDefaultView(itemsControl.ItemsSource)?.Refresh()));
    }

    private static void BeginInlineEdit(DataGrid grid, MouseButtonEventArgs e, DataGridColumn targetColumn)
    {
        var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
        if (cell is null || !ReferenceEquals(cell.Column, targetColumn))
            return;

        grid.CurrentCell = new DataGridCellInfo(cell.DataContext, targetColumn);
        if (!cell.IsEditing)
        {
            grid.BeginEdit(e);
            e.Handled = true;
        }
    }

    private static T? FindParent<T>(DependencyObject? dependencyObject) where T : DependencyObject
    {
        while (dependencyObject is not null)
        {
            if (dependencyObject is T target)
                return target;

            dependencyObject = System.Windows.Media.VisualTreeHelper.GetParent(dependencyObject);
        }

        return null;
    }
}
