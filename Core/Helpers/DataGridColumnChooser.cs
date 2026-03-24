using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attaches a column-visibility flyout to a button for a target <see cref="DataGrid"/>.
/// </summary>
public static class DataGridColumnChooser
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.RegisterAttached(
            "Target",
            typeof(DataGrid),
            typeof(DataGridColumnChooser),
            new PropertyMetadata(null, OnTargetChanged));

    public static DataGrid? GetTarget(DependencyObject obj) =>
        (DataGrid?)obj.GetValue(TargetProperty);

    public static void SetTarget(DependencyObject obj, DataGrid? value) =>
        obj.SetValue(TargetProperty, value);

    internal static IReadOnlyList<DataGridColumnChoice> GetChoices(DataGrid grid) =>
        GetChoices(grid.Columns.Cast<DataGridColumn>());

    internal static IReadOnlyList<DataGridColumnChoice> GetChoices(IEnumerable<DataGridColumn> columns)
    {
        var orderedColumns = columns.ToList();

        return orderedColumns
            .Where(IsEligibleColumn)
            .OrderBy(GetDisplayOrder)
            .ThenBy(column => orderedColumns.IndexOf(column))
            .Select(column => new DataGridColumnChoice(
                column,
                GetHeaderText(column.Header),
                column.Visibility == Visibility.Visible))
            .ToList();
    }

    internal static string GetHeaderText(object? header) => header switch
    {
        string text => text.Trim(),
        TextBlock textBlock => textBlock.Text.Trim(),
        _ => header?.ToString()?.Trim() ?? string.Empty
    };

    private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ButtonBase button)
            return;

        button.Click -= OnButtonClick;
        if (e.NewValue is DataGrid)
            button.Click += OnButtonClick;
    }

    private static void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ButtonBase button)
            return;

        var grid = GetTarget(button);
        if (grid is null)
            return;

        var menu = BuildContextMenu(grid);
        if (menu.Items.Count == 0)
            return;

        button.ContextMenu = menu;
        menu.Placement = PlacementMode.Bottom;
        menu.PlacementTarget = button;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private static ContextMenu BuildContextMenu(DataGrid grid)
    {
        var menu = new ContextMenu();

        foreach (var choice in GetChoices(grid))
        {
            var item = new MenuItem
            {
                Header = choice.Header,
                IsCheckable = true,
                IsChecked = choice.IsVisible,
                StaysOpenOnClick = true
            };

            item.Click += (_, _) =>
            {
                choice.Column.Visibility = item.IsChecked
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };

            menu.Items.Add(item);
        }

        return menu;
    }

    private static bool IsEligibleColumn(DataGridColumn column) =>
        !string.IsNullOrWhiteSpace(GetHeaderText(column.Header));

    private static int GetDisplayOrder(DataGridColumn column) =>
        column.DisplayIndex >= 0 ? column.DisplayIndex : int.MaxValue;
}

internal sealed record DataGridColumnChoice(
    DataGridColumn Column,
    string Header,
    bool IsVisible);
