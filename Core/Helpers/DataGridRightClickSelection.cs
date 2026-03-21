using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Selects the right-clicked <see cref="DataGridRow"/> before the
/// context menu opens, matching Windows Explorer behavior.
/// </summary>
public static class DataGridRightClickSelection
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridRightClickSelection),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;

        if ((bool)e.NewValue)
        {
            dataGrid.AddHandler(
                UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(OnPreviewMouseRightButtonDown),
                true);
        }
        else
        {
            dataGrid.RemoveHandler(
                UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(OnPreviewMouseRightButtonDown));
        }
    }

    private static void OnPreviewMouseRightButtonDown(
        object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid
            || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var row = FindAncestor<DataGridRow>(source);
        if (row is null
            || !ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(row), dataGrid))
        {
            return;
        }

        var cell = FindAncestor<DataGridCell>(source);
        if (cell is not null)
        {
            dataGrid.CurrentCell = new DataGridCellInfo(cell);
        }

        if (!row.IsSelected)
        {
            dataGrid.SelectedItem = row.Item;
        }

        if (!row.IsKeyboardFocusWithin)
        {
            row.Focus();
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = GetParent(current);
        }

        return null;
    }

    private static DependencyObject? GetParent(DependencyObject current) =>
        current switch
        {
            Visual or Visual3D => VisualTreeHelper.GetParent(current),
            FrameworkContentElement contentElement => contentElement.Parent,
            _ => null
        };
}
