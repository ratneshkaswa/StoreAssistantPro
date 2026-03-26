using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Persists per-grid column visibility, order, and widths into the shared user preference store.
/// </summary>
public static class DataGridLayoutPersistence
{
    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.RegisterAttached(
            "Key",
            typeof(string),
            typeof(DataGridLayoutPersistence),
            new PropertyMetadata(null, OnKeyChanged));

    private static readonly DependencyProperty BehaviorProperty =
        DependencyProperty.RegisterAttached(
            "Behavior",
            typeof(LayoutBehavior),
            typeof(DataGridLayoutPersistence),
            new PropertyMetadata(null));

    public static string? GetKey(DependencyObject obj) => (string?)obj.GetValue(KeyProperty);

    public static void SetKey(DependencyObject obj, string? value) => obj.SetValue(KeyProperty, value);

    internal static DataGridLayoutState CaptureLayout(IEnumerable<DataGridColumn> columns) => new()
    {
        Columns =
        [
            .. columns
                .Where(column => !string.IsNullOrWhiteSpace(DataGridColumnChooser.GetHeaderText(column.Header)))
                .OrderBy(column => column.DisplayIndex >= 0 ? column.DisplayIndex : int.MaxValue)
                .Select(column => new DataGridColumnLayoutState
                {
                    Header = DataGridColumnChooser.GetHeaderText(column.Header),
                    DisplayIndex = column.DisplayIndex,
                    IsVisible = column.Visibility == Visibility.Visible,
                    Width = GetPersistedWidth(column)
                })
        ]
    };

    internal static void ApplyLayout(IList<DataGridColumn> columns, DataGridLayoutState? layout)
    {
        if (layout?.Columns.Count is not > 0)
            return;

        var columnMap = columns
            .Where(column => !string.IsNullOrWhiteSpace(DataGridColumnChooser.GetHeaderText(column.Header)))
            .ToDictionary(
                column => DataGridColumnChooser.GetHeaderText(column.Header),
                StringComparer.OrdinalIgnoreCase);

        foreach (var savedColumn in layout.Columns)
        {
            if (!columnMap.TryGetValue(savedColumn.Header, out var column))
                continue;

            column.Visibility = savedColumn.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            if (savedColumn.Width is double width && width > 0d)
                column.Width = new DataGridLength(width);
        }

        var orderedColumns = layout.Columns
            .Where(savedColumn => savedColumn.DisplayIndex >= 0)
            .OrderBy(savedColumn => savedColumn.DisplayIndex)
            .ToList();

        var nextDisplayIndex = 0;
        foreach (var savedColumn in orderedColumns)
        {
            if (!columnMap.TryGetValue(savedColumn.Header, out var column))
                continue;

            try
            {
                column.DisplayIndex = nextDisplayIndex++;
            }
            catch
            {
                // Ignore invalid display-index transitions and keep the current layout intact.
            }
        }
    }

    private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
            return;

        if (grid.GetValue(BehaviorProperty) is LayoutBehavior existingBehavior)
        {
            existingBehavior.Detach();
            grid.ClearValue(BehaviorProperty);
        }

        if (e.NewValue is not string key || string.IsNullOrWhiteSpace(key))
            return;

        grid.SetValue(BehaviorProperty, new LayoutBehavior(grid, key.Trim()));
    }

    private static double? GetPersistedWidth(DataGridColumn column)
    {
        if (double.IsNaN(column.ActualWidth) || column.ActualWidth <= 0)
            return null;

        return Math.Round(column.ActualWidth, 2);
    }

    private sealed class LayoutBehavior
    {
        private static readonly DependencyPropertyDescriptor? VisibilityDescriptor =
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.VisibilityProperty, typeof(DataGridColumn));

        private static readonly DependencyPropertyDescriptor? DisplayIndexDescriptor =
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.DisplayIndexProperty, typeof(DataGridColumn));

        private static readonly DependencyPropertyDescriptor? WidthDescriptor =
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));

        private readonly DataGrid _grid;
        private readonly string _key;
        private bool _isApplying;
        private bool _saveQueued;

        public LayoutBehavior(DataGrid grid, string key)
        {
            _grid = grid;
            _key = key;
            _grid.Loaded += OnLoaded;
            _grid.Unloaded += OnUnloaded;
            _grid.ColumnReordered += OnColumnReordered;

            if (_grid.Columns is INotifyCollectionChanged collection)
                collection.CollectionChanged += OnColumnsCollectionChanged;

            SubscribeColumns(_grid.Columns);
        }

        public void Detach()
        {
            _grid.Loaded -= OnLoaded;
            _grid.Unloaded -= OnUnloaded;
            _grid.ColumnReordered -= OnColumnReordered;

            if (_grid.Columns is INotifyCollectionChanged collection)
                collection.CollectionChanged -= OnColumnsCollectionChanged;

            UnsubscribeColumns(_grid.Columns);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isApplying = true;
            try
            {
                ApplyLayout(_grid.Columns, UserPreferencesStore.GetDataGridLayout(_key));
            }
            finally
            {
                _isApplying = false;
            }

            QueueSave();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => SaveNow();

        private void OnColumnReordered(object? sender, DataGridColumnEventArgs e) => QueueSave();

        private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is IEnumerable oldItems)
                UnsubscribeColumns(oldItems.OfType<DataGridColumn>());

            if (e.NewItems is IEnumerable newItems)
                SubscribeColumns(newItems.OfType<DataGridColumn>());

            QueueSave();
        }

        private void OnColumnPropertyChanged(object? sender, EventArgs e) => QueueSave();

        private void SubscribeColumns(IEnumerable<DataGridColumn> columns)
        {
            foreach (var column in columns)
            {
                VisibilityDescriptor?.AddValueChanged(column, OnColumnPropertyChanged);
                DisplayIndexDescriptor?.AddValueChanged(column, OnColumnPropertyChanged);
                WidthDescriptor?.AddValueChanged(column, OnColumnPropertyChanged);
            }
        }

        private void UnsubscribeColumns(IEnumerable<DataGridColumn> columns)
        {
            foreach (var column in columns)
            {
                VisibilityDescriptor?.RemoveValueChanged(column, OnColumnPropertyChanged);
                DisplayIndexDescriptor?.RemoveValueChanged(column, OnColumnPropertyChanged);
                WidthDescriptor?.RemoveValueChanged(column, OnColumnPropertyChanged);
            }
        }

        private void QueueSave()
        {
            if (_isApplying || _saveQueued)
                return;

            _saveQueued = true;
            _grid.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _saveQueued = false;
                SaveNow();
            }));
        }

        private void SaveNow()
        {
            if (_isApplying)
                return;

            UserPreferencesStore.SetDataGridLayout(_key, CaptureLayout(_grid.Columns));
        }
    }
}
