using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using StoreAssistantPro.Core.Controls;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Shows a shared empty-state overlay for DataGrids when they have no rows.
/// The content can be overridden per grid through attached properties.
/// </summary>
public static class DataGridEmptyState
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridEmptyState),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached(
            "Icon",
            typeof(string),
            typeof(DataGridEmptyState),
            new PropertyMetadata(string.Empty, OnPresentationPropertyChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.RegisterAttached(
            "Title",
            typeof(string),
            typeof(DataGridEmptyState),
            new PropertyMetadata("No items to display", OnPresentationPropertyChanged));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.RegisterAttached(
            "Description",
            typeof(string),
            typeof(DataGridEmptyState),
            new PropertyMetadata(string.Empty, OnPresentationPropertyChanged));

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.RegisterAttached(
            "ActionText",
            typeof(string),
            typeof(DataGridEmptyState),
            new PropertyMetadata(null, OnPresentationPropertyChanged));

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.RegisterAttached(
            "ActionCommand",
            typeof(ICommand),
            typeof(DataGridEmptyState),
            new PropertyMetadata(null, OnPresentationPropertyChanged));

    private static readonly DependencyProperty TrackerProperty =
        DependencyProperty.RegisterAttached(
            "Tracker",
            typeof(Tracker),
            typeof(DataGridEmptyState));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    public static string GetIcon(DependencyObject obj) =>
        (string)obj.GetValue(IconProperty);

    public static void SetIcon(DependencyObject obj, string value) =>
        obj.SetValue(IconProperty, value);

    public static string GetTitle(DependencyObject obj) =>
        (string)obj.GetValue(TitleProperty);

    public static void SetTitle(DependencyObject obj, string value) =>
        obj.SetValue(TitleProperty, value);

    public static string GetDescription(DependencyObject obj) =>
        (string)obj.GetValue(DescriptionProperty);

    public static void SetDescription(DependencyObject obj, string value) =>
        obj.SetValue(DescriptionProperty, value);

    public static string? GetActionText(DependencyObject obj) =>
        (string?)obj.GetValue(ActionTextProperty);

    public static void SetActionText(DependencyObject obj, string? value) =>
        obj.SetValue(ActionTextProperty, value);

    public static ICommand? GetActionCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(ActionCommandProperty);

    public static void SetActionCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(ActionCommandProperty, value);

    private static void OnIsEnabledChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            var tracker = GetOrCreateTracker(dataGrid);
            tracker.Attach();
            tracker.Refresh();
        }
        else
        {
            GetTracker(dataGrid)?.Detach();
            dataGrid.ClearValue(TrackerProperty);
        }
    }

    private static void OnPresentationPropertyChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid dataGrid)
        {
            GetTracker(dataGrid)?.Refresh();
        }
    }

    private static Tracker? GetTracker(DependencyObject obj) =>
        obj.GetValue(TrackerProperty) as Tracker;

    private static Tracker GetOrCreateTracker(DataGrid dataGrid)
    {
        if (GetTracker(dataGrid) is Tracker tracker)
        {
            return tracker;
        }

        tracker = new Tracker(dataGrid);
        dataGrid.SetValue(TrackerProperty, tracker);
        return tracker;
    }

    private sealed class Tracker
    {
        private readonly DataGrid _dataGrid;
        private INotifyCollectionChanged? _items;
        private DataGridEmptyStateAdorner? _adorner;
        private AdornerLayer? _adornerLayer;
        private bool _isAttached;

        public Tracker(DataGrid dataGrid) => _dataGrid = dataGrid;

        public void Attach()
        {
            if (_isAttached)
            {
                return;
            }

            _dataGrid.Loaded += OnLoaded;
            _dataGrid.Unloaded += OnUnloaded;
            _dataGrid.IsVisibleChanged += OnIsVisibleChanged;
            _dataGrid.SizeChanged += OnSizeChanged;

            if (_dataGrid.Items is INotifyCollectionChanged items)
            {
                _items = items;
                _items.CollectionChanged += OnItemsChanged;
            }

            _isAttached = true;
        }

        public void Detach()
        {
            if (!_isAttached)
            {
                return;
            }

            _dataGrid.Loaded -= OnLoaded;
            _dataGrid.Unloaded -= OnUnloaded;
            _dataGrid.IsVisibleChanged -= OnIsVisibleChanged;
            _dataGrid.SizeChanged -= OnSizeChanged;

            if (_items is not null)
            {
                _items.CollectionChanged -= OnItemsChanged;
                _items = null;
            }

            RemoveAdorner();
            _isAttached = false;
        }

        public void Refresh()
        {
            EnsureAdorner();
            _adorner?.UpdateContent();
            UpdateVisibility();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => Refresh();

        private void OnUnloaded(object sender, RoutedEventArgs e) => RemoveAdorner();

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            Refresh();

        private void OnIsVisibleChanged(
            object sender, DependencyPropertyChangedEventArgs e) => UpdateVisibility();

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) =>
            _adorner?.InvalidateArrange();

        private void EnsureAdorner()
        {
            if (_adorner is not null || !_dataGrid.IsLoaded)
            {
                return;
            }

            _adornerLayer = AdornerLayer.GetAdornerLayer(_dataGrid);
            if (_adornerLayer is null)
            {
                return;
            }

            _adorner = new DataGridEmptyStateAdorner(_dataGrid);
            _adornerLayer.Add(_adorner);
        }

        private void UpdateVisibility()
        {
            if (_adorner is null)
            {
                return;
            }

            _adorner.Visibility =
                _dataGrid.IsVisible && !_dataGrid.HasItems
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void RemoveAdorner()
        {
            if (_adornerLayer is null || _adorner is null)
            {
                return;
            }

            _adornerLayer.Remove(_adorner);
            _adorner = null;
            _adornerLayer = null;
        }
    }

    private sealed class DataGridEmptyStateAdorner : Adorner
    {
        private readonly VisualCollection _visuals;
        private readonly EmptyStateOverlay _overlay;

        public DataGridEmptyStateAdorner(DataGrid dataGrid)
            : base(dataGrid)
        {
            IsHitTestVisible = true;
            _overlay = new EmptyStateOverlay();
            _visuals = new VisualCollection(this) { _overlay };
            UpdateContent();
        }

        protected override int VisualChildrenCount => _visuals.Count;

        public void UpdateContent()
        {
            var dataGrid = (DataGrid)AdornedElement;

            _overlay.Icon = GetIcon(dataGrid);
            _overlay.Title = GetTitle(dataGrid);
            _overlay.Description = GetDescription(dataGrid);
            _overlay.ActionText = GetActionText(dataGrid);
            _overlay.ActionCommand = GetActionCommand(dataGrid);
            _overlay.ItemCount = dataGrid.HasItems ? 1 : 0;
        }

        protected override Visual GetVisualChild(int index) => _visuals[index];

        protected override Size MeasureOverride(Size constraint)
        {
            _overlay.Measure(constraint);
            return constraint;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var headerHeight = GetColumnHeaderHeight((DataGrid)AdornedElement);
            var contentHeight = Math.Max(0, finalSize.Height - headerHeight);

            _overlay.Arrange(new Rect(0, headerHeight, finalSize.Width, contentHeight));
            return finalSize;
        }

        private static double GetColumnHeaderHeight(DataGrid dataGrid)
        {
            var presenter = FindDescendant<DataGridColumnHeadersPresenter>(dataGrid);
            if (presenter is not null && presenter.ActualHeight > 0)
            {
                return presenter.ActualHeight;
            }

            return double.IsNaN(dataGrid.ColumnHeaderHeight)
                ? 36
                : Math.Max(0, dataGrid.ColumnHeaderHeight);
        }

        private static T? FindDescendant<T>(DependencyObject? current)
            where T : DependencyObject
        {
            if (current is null)
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(current, i);
                if (child is T match)
                {
                    return match;
                }

                var nestedMatch = FindDescendant<T>(child);
                if (nestedMatch is not null)
                {
                    return nestedMatch;
                }
            }

            return null;
        }
    }
}
