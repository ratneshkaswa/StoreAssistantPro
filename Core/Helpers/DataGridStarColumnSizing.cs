using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Keeps star-sized DataGrid columns distributed proportionally after
/// fixed-width columns have taken the space they need.
/// </summary>
public static class DataGridStarColumnSizing
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridStarColumnSizing),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty SyncStateProperty =
        DependencyProperty.RegisterAttached(
            "SyncState",
            typeof(SyncState),
            typeof(DataGridStarColumnSizing),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;

        if ((bool)e.NewValue)
        {
            if (dataGrid.GetValue(SyncStateProperty) is not SyncState)
            {
                var state = new SyncState(dataGrid);
                dataGrid.SetValue(SyncStateProperty, state);
                state.Attach();
            }
        }
        else if (dataGrid.GetValue(SyncStateProperty) is SyncState state)
        {
            state.Detach();
            dataGrid.ClearValue(SyncStateProperty);
        }
    }

    private sealed class SyncState
    {
        private readonly DataGrid _dataGrid;
        private readonly Dictionary<DataGridColumn, double> _starWeights = [];
        private bool _syncQueued;

        public SyncState(DataGrid dataGrid) => _dataGrid = dataGrid;

        public void Attach()
        {
            CaptureStarColumns();
            _dataGrid.Loaded += OnLayoutChanged;
            _dataGrid.SizeChanged += OnLayoutChanged;
            _dataGrid.Unloaded += OnUnloaded;
            _dataGrid.Columns.CollectionChanged += OnColumnsCollectionChanged;
            QueueSync();
        }

        public void Detach()
        {
            _dataGrid.Loaded -= OnLayoutChanged;
            _dataGrid.SizeChanged -= OnLayoutChanged;
            _dataGrid.Unloaded -= OnUnloaded;
            _dataGrid.Columns.CollectionChanged -= OnColumnsCollectionChanged;
            _starWeights.Clear();
        }

        private void OnLayoutChanged(object? sender, RoutedEventArgs e) => QueueSync();

        private void OnLayoutChanged(object? sender, SizeChangedEventArgs e) => QueueSync();

        private void OnUnloaded(object? sender, RoutedEventArgs e) => Detach();

        private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CaptureStarColumns();
            QueueSync();
        }

        private void QueueSync()
        {
            if (_syncQueued)
                return;

            _syncQueued = true;
            _dataGrid.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _syncQueued = false;
                ApplyProportionalWidths();
            }));
        }

        private void CaptureStarColumns()
        {
            var liveColumns = _dataGrid.Columns.ToHashSet();

            foreach (var column in _dataGrid.Columns)
            {
                if (column.Width.UnitType == DataGridLengthUnitType.Star)
                {
                    _starWeights[column] = column.Width.Value > 0 ? column.Width.Value : 1d;
                }
            }

            foreach (var removed in _starWeights.Keys.Where(column => !liveColumns.Contains(column)).ToList())
            {
                _starWeights.Remove(removed);
            }
        }

        private void ApplyProportionalWidths()
        {
            if (_dataGrid.ActualWidth <= 0 || _starWeights.Count == 0)
                return;

            var visibleStarColumns = _starWeights
                .Where(pair => pair.Key.Visibility == Visibility.Visible)
                .ToList();

            if (visibleStarColumns.Count == 0)
                return;

            var fixedWidth = _dataGrid.Columns
                .Where(column => column.Visibility == Visibility.Visible && !_starWeights.ContainsKey(column))
                .Sum(column => column.ActualWidth);

            var chromeWidth = _dataGrid.RowHeaderWidth + SystemParameters.VerticalScrollBarWidth + 24;
            var availableWidth = _dataGrid.ActualWidth - fixedWidth - chromeWidth;
            if (availableWidth <= 0)
                return;

            var totalWeight = visibleStarColumns.Sum(pair => Math.Max(pair.Value, 1d));
            if (totalWeight <= 0)
                return;

            foreach (var (column, weight) in visibleStarColumns)
            {
                var proportionalWidth = Math.Max(64d, availableWidth * (Math.Max(weight, 1d) / totalWeight));
                column.Width = new DataGridLength(proportionalWidth, DataGridLengthUnitType.Pixel);
            }
        }
    }
}
