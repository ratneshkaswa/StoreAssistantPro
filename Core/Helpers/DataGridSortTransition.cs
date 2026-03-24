using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Applies a short row crossfade after DataGrid sorting completes.
/// </summary>
public static class DataGridSortTransition
{
    private const double StartingOpacity = 0.58;
    private static readonly Duration TransitionDuration = new(TimeSpan.FromMilliseconds(100));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridSortTransition),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty IsHookedProperty =
        DependencyProperty.RegisterAttached(
            "IsHooked",
            typeof(bool),
            typeof(DataGridSortTransition),
            new PropertyMetadata(false));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
        {
            return;
        }

        var enabled = (bool)e.NewValue;
        var isHooked = (bool)dataGrid.GetValue(IsHookedProperty);

        if (enabled && !isHooked)
        {
            dataGrid.Sorting += OnDataGridSorting;
            dataGrid.Unloaded += OnDataGridUnloaded;
            dataGrid.SetValue(IsHookedProperty, true);
        }
        else if (!enabled && isHooked)
        {
            dataGrid.Sorting -= OnDataGridSorting;
            dataGrid.Unloaded -= OnDataGridUnloaded;
            dataGrid.SetValue(IsHookedProperty, false);
        }
    }

    private static void OnDataGridUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            dataGrid.Sorting -= OnDataGridSorting;
            dataGrid.Unloaded -= OnDataGridUnloaded;
            dataGrid.SetValue(IsHookedProperty, false);
        }
    }

    private static void OnDataGridSorting(object sender, DataGridSortingEventArgs e)
    {
        if (sender is not DataGrid dataGrid || !GetIsEnabled(dataGrid))
        {
            return;
        }

        dataGrid.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => AnimateSortedRows(dataGrid)));
    }

    private static void AnimateSortedRows(DataGrid dataGrid)
    {
        FrameworkElement? rowsPresenter = FindVisualChild<ItemsPresenter>(dataGrid);
        rowsPresenter ??= FindVisualChild<ScrollContentPresenter>(dataGrid);

        UIElement target = rowsPresenter ?? dataGrid;
        target.BeginAnimation(UIElement.OpacityProperty, null);
        target.Opacity = StartingOpacity;

        var fadeAnimation = new DoubleAnimation(StartingOpacity, 1d, TransitionDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        target.BeginAnimation(UIElement.OpacityProperty, fadeAnimation, HandoffBehavior.SnapshotAndReplace);
    }

    private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent is null)
        {
            return null;
        }

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
