using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Categories.ViewModels;

namespace StoreAssistantPro.Modules.Categories.Views;

public partial class CategoryManagementView : UserControl
{
    private readonly Dictionary<int, string> _pendingCategoryTypeNames = [];
    private readonly Dictionary<int, string> _pendingCategoryNames = [];

    public CategoryManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CategoryManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }

    private void OnCategoryTypesGridPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        BeginInlineEdit((DataGrid)sender, e, CategoryTypeNameColumn);

    private void OnCategoriesGridPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        BeginInlineEdit((DataGrid)sender, e, CategoryNameColumn);

    private void OnCategoryTypesGridBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (ReferenceEquals(e.Column, CategoryTypeNameColumn) && e.Row.Item is CategoryType categoryType)
            _pendingCategoryTypeNames[categoryType.Id] = categoryType.Name;
    }

    private void OnCategoriesGridBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (ReferenceEquals(e.Column, CategoryNameColumn) && e.Row.Item is Category category)
            _pendingCategoryNames[category.Id] = category.Name;
    }

    private async void OnCategoryTypesGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit
            || !ReferenceEquals(e.Column, CategoryTypeNameColumn)
            || e.Row.Item is not CategoryType categoryType
            || e.EditingElement is not TextBox textBox
            || DataContext is not CategoryManagementViewModel vm)
        {
            return;
        }

        var originalName = _pendingCategoryTypeNames.TryGetValue(categoryType.Id, out var value)
            ? value
            : categoryType.Name;
        _pendingCategoryTypeNames.Remove(categoryType.Id);

        var success = await vm.TryInlineRenameTypeAsync(categoryType, textBox.Text, originalName);
        if (!success)
            RefreshItemsView((DataGrid)sender);
    }

    private async void OnCategoriesGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit
            || !ReferenceEquals(e.Column, CategoryNameColumn)
            || e.Row.Item is not Category category
            || e.EditingElement is not TextBox textBox
            || DataContext is not CategoryManagementViewModel vm)
        {
            return;
        }

        var originalName = _pendingCategoryNames.TryGetValue(category.Id, out var value)
            ? value
            : category.Name;
        _pendingCategoryNames.Remove(category.Id);

        var success = await vm.TryInlineRenameCategoryAsync(category, textBox.Text, originalName);
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
