using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Debug-only attached behavior that validates enterprise scroll policy
/// on every <see cref="Window"/> at load time.
/// <para>
/// <b>Detects:</b>
/// <list type="bullet">
///   <item><c>ScrollViewer</c> wrapping the entire window content
///         (immediate child of the <c>Window</c>, or immediate child
///         of the root <c>Grid</c>/<c>DockPanel</c>).</item>
///   <item><c>ScrollViewer</c> wrapping a <c>Grid</c> that contains
///         only form fields (no <c>DataGrid</c>, <c>ListView</c>,
///         <c>ItemsControl</c>).</item>
/// </list>
/// </para>
/// <para>
/// <b>Allows:</b> <c>ScrollViewer</c> around <c>DataGrid</c>,
/// <c>ListView</c>, <c>ItemsControl</c>, <c>ContentControl</c>,
/// or any element whose parent is a star-sized <c>Grid</c> row.
/// </para>
/// <para>
/// <b>Activation (GlobalStyles.xaml):</b>
/// </para>
/// <code>
/// &lt;Style TargetType="Window"&gt;
///     &lt;Setter Property="h:LayoutDiagnostics.IsEnabled" Value="True"/&gt;
/// &lt;/Style&gt;
/// </code>
/// <para>
/// Warnings appear in the Visual Studio <b>Output → Debug</b> pane.
/// The behavior compiles to a no-op in Release builds.
/// </para>
/// </summary>
public static class LayoutDiagnostics
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(LayoutDiagnostics),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
#if DEBUG
        if (d is Window window && e.NewValue is true)
            window.Loaded += OnWindowLoaded;
#endif
    }

#if DEBUG
    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window)
            return;

        var windowName = window.GetType().Name;
        var content = window.Content as DependencyObject;
        if (content is null)
            return;

        // Check 1: Is the Window's direct content a ScrollViewer?
        if (content is ScrollViewer)
        {
            Warn(windowName, "Window.Content is a ScrollViewer — entire window scrolls. "
                + "Use Grid with star-sized rows instead.");
            return;
        }

        // Check 2: Is the first child of the root panel a ScrollViewer?
        if (content is Panel rootPanel)
        {
            foreach (UIElement child in rootPanel.Children)
            {
                if (child is ScrollViewer sv && IsWrappingEntireContent(rootPanel, sv))
                {
                    Warn(windowName, $"Root panel contains a ScrollViewer at row "
                        + $"{Grid.GetRow(sv)} that wraps the entire content area. "
                        + "ScrollViewer should only wrap data collections (DataGrid, "
                        + "ListView, ItemsControl).");
                }
            }
        }

        // Check 3: Walk the visual tree for ScrollViewers wrapping only form controls
        WalkTree(content, windowName, depth: 0);
    }

    private static void WalkTree(DependencyObject parent, string windowName, int depth)
    {
        if (depth > 20)
            return;

        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is ScrollViewer sv && ContainsOnlyFormControls(sv))
            {
                Warn(windowName, $"ScrollViewer at depth {depth} wraps only form "
                    + "controls (no DataGrid/ListView/ItemsControl). Consider "
                    + "removing it and using Grid row sizing instead.");
            }

            WalkTree(child, windowName, depth + 1);
        }
    }

    /// <summary>
    /// A ScrollViewer "wraps entire content" when it is the only non-collapsed
    /// child or occupies the star-sized row of the root Grid.
    /// </summary>
    private static bool IsWrappingEntireContent(Panel rootPanel, ScrollViewer sv)
    {
        if (rootPanel is not Grid grid)
        {
            // In a non-Grid panel, a ScrollViewer as the only child wraps everything
            var visibleCount = 0;
            foreach (UIElement child in rootPanel.Children)
            {
                if (child.Visibility != Visibility.Collapsed)
                    visibleCount++;
            }
            return visibleCount == 1;
        }

        // In a Grid: check if the ScrollViewer is in row 0 and spans most rows,
        // or is the only visible child
        var row = Grid.GetRow(sv);
        var rowSpan = Grid.GetRowSpan(sv);

        // Spans all rows → wrapping everything
        if (rowSpan >= grid.RowDefinitions.Count && grid.RowDefinitions.Count > 0)
            return true;

        // In row 0 with no other rows → wrapping everything
        if (row == 0 && grid.RowDefinitions.Count <= 1)
            return true;

        return false;
    }

    /// <summary>
    /// Returns true when a ScrollViewer's content tree contains no data controls
    /// (DataGrid, ListView, ListBox, ItemsControl with ItemsSource binding).
    /// </summary>
    private static bool ContainsOnlyFormControls(ScrollViewer sv)
    {
        return !ContainsDataControl(sv);
    }

    private static bool ContainsDataControl(DependencyObject parent)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is DataGrid or ListView or ListBox)
                return true;

            if (child is ItemsControl ic and not ComboBox && ic.ItemsSource is not null)
                return true;

            if (child is ContentControl)
                return true;

            if (ContainsDataControl(child))
                return true;
        }

        return false;
    }

    private static void Warn(string windowName, string message)
    {
        var text = $"[LayoutDiagnostics] {windowName}: {message}";
        Debug.WriteLine(text);
        Trace.TraceWarning(text);
    }
#endif
}
