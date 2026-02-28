using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Reusable empty-state overlay that displays an icon, title, short
/// description, and an optional action button when a data collection
/// contains zero items.
/// <para>
/// <b>Visual layout:</b>
/// <code>
/// ┌──────────────────────────────────────────┐
/// │                                          │
/// │              📦  (icon, 28 pt)           │
/// │         No products yet                  │
/// │   Add your first product to get started. │
/// │         [ ➕ Add Product ]               │
/// │                                          │
/// └──────────────────────────────────────────┘
/// </code>
/// </para>
/// <para>
/// <b>Auto-visibility:</b> Bind <see cref="ItemCount"/> to the
/// collection's <c>Count</c> property. The overlay automatically
/// shows when <c>ItemCount == 0</c> and collapses otherwise.
/// </para>
/// <para>
/// <b>Design tokens consumed:</b>
/// </para>
/// <list type="table">
///   <listheader><term>Token</term><description>Role</description></listheader>
///   <item><term>FontSizeIconLarge</term><description>Icon text size (36 px).</description></item>
///   <item><term>FontSizeSectionHeader</term><description>Title size (16 px).</description></item>
///   <item><term>FontSizeBody</term><description>Description size (13 px).</description></item>
///   <item><term>FluentTextPrimary</term><description>Title foreground.</description></item>
///   <item><term>FluentTextTertiary</term><description>Icon + description foreground.</description></item>
///   <item><term>EmptyStatePadding</term><description>Vertical centering padding.</description></item>
/// </list>
/// <para>
/// <b>Placement:</b> Place as a sibling overlay to the
/// <c>DataGrid</c> inside the same <c>Grid</c> cell.
/// <c>IsHitTestVisible="False"</c> is set automatically so the
/// overlay never blocks interaction with underlying controls.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;Grid&gt;
///     &lt;DataGrid Style="{StaticResource EnterpriseDataGridStyle}"
///               ItemsSource="{Binding Products}" .../&gt;
///     &lt;controls:EmptyStateOverlay
///         Icon="📦"
///         Title="No products yet"
///         Description="Add your first product to get started."
///         ActionText="➕ Add Product"
///         ActionCommand="{Binding ShowAddFormCommand}"
///         ItemCount="{Binding Products.Count}"/&gt;
/// &lt;/Grid&gt;
/// </code>
/// </summary>
public class EmptyStateOverlay : Control
{
    // ── Icon DP ───────────────────────────────────────────────────

    /// <summary>
    /// Emoji or text icon displayed above the title (e.g. "📦").
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon), typeof(string), typeof(EmptyStateOverlay),
            new PropertyMetadata("📋"));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    // ── Title DP ──────────────────────────────────────────────────

    /// <summary>
    /// Bold title text (e.g. "No products yet").
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(EmptyStateOverlay),
            new PropertyMetadata("No items found"));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // ── Description DP ────────────────────────────────────────────

    /// <summary>
    /// Short explanatory text below the title.
    /// When empty or null, the description row collapses.
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(EmptyStateOverlay),
            new PropertyMetadata(string.Empty));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    // ── ActionText DP ─────────────────────────────────────────────

    /// <summary>
    /// Optional button label. When null or empty the button collapses.
    /// </summary>
    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(
            nameof(ActionText), typeof(string), typeof(EmptyStateOverlay),
            new PropertyMetadata(null));

    public string? ActionText
    {
        get => (string?)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    // ── ActionCommand DP ──────────────────────────────────────────

    /// <summary>
    /// Command executed when the action button is clicked.
    /// The button is only visible when both <see cref="ActionText"/>
    /// and <see cref="ActionCommand"/> are set.
    /// </summary>
    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(
            nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateOverlay));

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    // ── ItemCount DP ──────────────────────────────────────────────

    /// <summary>
    /// Bind to the data collection's <c>Count</c> property.
    /// The overlay is visible when <c>ItemCount == 0</c> and
    /// collapsed otherwise.
    /// </summary>
    public static readonly DependencyProperty ItemCountProperty =
        DependencyProperty.Register(
            nameof(ItemCount), typeof(int), typeof(EmptyStateOverlay),
            new PropertyMetadata(0, OnItemCountChanged));

    public int ItemCount
    {
        get => (int)GetValue(ItemCountProperty);
        set => SetValue(ItemCountProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────

    static EmptyStateOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(EmptyStateOverlay),
            new FrameworkPropertyMetadata(typeof(EmptyStateOverlay)));

        FocusableProperty.OverrideMetadata(
            typeof(EmptyStateOverlay),
            new FrameworkPropertyMetadata(false));

        // Note: IsHitTestVisible is NOT set to false here.
        // The overlay is only visible when ItemCount == 0 (empty grid),
        // so it never blocks meaningful interaction underneath.
        // Keeping hit-testing enabled allows the optional action button
        // to receive clicks.
    }

    // ── Auto-visibility ───────────────────────────────────────────

    private static void OnItemCountChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EmptyStateOverlay overlay)
            overlay.UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        Visibility = ItemCount == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc/>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisibility();
    }
}
