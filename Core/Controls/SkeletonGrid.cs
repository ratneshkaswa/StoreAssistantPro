using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Skeleton placeholder grid that displays shimmering rows to indicate
/// data is loading. Automatically shows when <see cref="IsActive"/> is
/// <c>true</c> and collapses otherwise.
/// <para>
/// <b>Visual layout:</b>
/// <code>
/// ┌──────────────────────────────────────┐
/// │  ██████████████████████████  (100%)  │
/// │  ████████████████████  (80%)         │
/// │  ██████████████████████████  (100%)  │
/// │  ██████████████  (60%)               │
/// │  ████████████████████  (80%)         │
/// └──────────────────────────────────────┘
/// </code>
/// </para>
/// <para>
/// <b>Auto-visibility:</b> Bind <see cref="IsActive"/> to
/// <c>BaseViewModel.IsLoading</c>. The skeleton shows during load
/// and collapses when data arrives, revealing the real content beneath.
/// </para>
/// <para>
/// <b>Non-blocking:</b> The shimmer animation uses a GPU-composited
/// <c>TranslateTransform</c> on a <c>LinearGradientBrush</c>, which
/// runs on the render thread and never blocks the UI dispatcher.
/// </para>
///
/// <para><b>Usage (overlay on a DataGrid):</b></para>
/// <code>
/// &lt;Grid&gt;
///     &lt;DataGrid .../&gt;
///     &lt;controls:SkeletonGrid
///         IsActive="{Binding IsLoading}"
///         RowCount="6"/&gt;
/// &lt;/Grid&gt;
/// </code>
/// </summary>
public class SkeletonGrid : Control
{
    // ── IsActive DP ───────────────────────────────────────────────

    /// <summary>
    /// When <c>true</c> the skeleton is visible and animating.
    /// Bind to <c>BaseViewModel.IsLoading</c>.
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive), typeof(bool), typeof(SkeletonGrid),
            new PropertyMetadata(false, OnIsActiveChanged));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    // ── RowCount DP ───────────────────────────────────────────────

    /// <summary>
    /// Number of skeleton rows to display. Default: 5.
    /// </summary>
    public static readonly DependencyProperty RowCountProperty =
        DependencyProperty.Register(
            nameof(RowCount), typeof(int), typeof(SkeletonGrid),
            new PropertyMetadata(5));

    public int RowCount
    {
        get => (int)GetValue(RowCountProperty);
        set => SetValue(RowCountProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────

    static SkeletonGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SkeletonGrid),
            new FrameworkPropertyMetadata(typeof(SkeletonGrid)));

        FocusableProperty.OverrideMetadata(
            typeof(SkeletonGrid),
            new FrameworkPropertyMetadata(false));

        IsHitTestVisibleProperty.OverrideMetadata(
            typeof(SkeletonGrid),
            new FrameworkPropertyMetadata(false));
    }

    // ── Auto-visibility ───────────────────────────────────────────

    private static void OnIsActiveChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SkeletonGrid grid)
            grid.Visibility = (bool)e.NewValue
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    /// <inheritdoc/>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Visibility = IsActive ? Visibility.Visible : Visibility.Collapsed;
    }
}
