using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Enterprise standard page layout control that enforces the canonical
/// row structure for data management pages.
/// <para>
/// <b>Row structure:</b>
/// <list type="number">
///   <item>TipBanner (Auto) — <see cref="TipBannerContent"/></item>
///   <item>Toolbar (Auto) — <see cref="ToolbarContent"/></item>
///   <item>MainContent (*) — <c>Content</c> (star-sized, fills remaining space)</item>
///   <item>Messages (Auto) — auto-bound to <c>ErrorMessage</c> / <c>SuccessMessage</c></item>
///   <item>BottomForm (Auto) — <see cref="BottomFormContent"/></item>
///   <item>StatusBar (Auto) — <see cref="StatusBarContent"/></item>
/// </list>
/// </para>
/// <para>
/// <b>Built-in features:</b>
/// <list type="bullet">
///   <item>Standard <c>PagePadding</c> margin on root grid.</item>
///   <item>Loading overlay on main content area (bound to <c>IsLoading</c>).</item>
///   <item>Error/success message bar (bound to <c>ErrorMessage</c> / <c>SuccessMessage</c>).</item>
///   <item><c>MinHeight="100"</c> on main content area (prevents DataGrid collapse).</item>
///   <item>Empty slots auto-collapse (no wasted vertical space).</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// </para>
/// <code>
/// &lt;UserControl x:Class="StoreAssistantPro.Modules.Foo.Views.FooView"&gt;
///     &lt;controls:EnterprisePageLayout&gt;
///         &lt;controls:EnterprisePageLayout.TipBannerContent&gt;
///             &lt;controls:InlineTipBanner .../&gt;
///         &lt;/controls:EnterprisePageLayout.TipBannerContent&gt;
///         &lt;controls:EnterprisePageLayout.ToolbarContent&gt;
///             &lt;Border Style="{StaticResource SectionCardStyle}"&gt;...&lt;/Border&gt;
///         &lt;/controls:EnterprisePageLayout.ToolbarContent&gt;
///         &lt;!-- Main content (DataGrid) placed as Content --&gt;
///         &lt;Border Style="{StaticResource SectionCardStyle}" ClipToBounds="True"&gt;
///             &lt;DataGrid .../&gt;
///         &lt;/Border&gt;
///         &lt;controls:EnterprisePageLayout.BottomFormContent&gt;
///             &lt;!-- Collapsible add/edit forms --&gt;
///         &lt;/controls:EnterprisePageLayout.BottomFormContent&gt;
///     &lt;/controls:EnterprisePageLayout&gt;
/// &lt;/UserControl&gt;
/// </code>
/// </summary>
public class EnterprisePageLayout : ContentControl
{
    /// <summary>Tip banner slot (Row 0, Auto).</summary>
    public static readonly DependencyProperty TipBannerContentProperty =
        DependencyProperty.Register(
            nameof(TipBannerContent), typeof(object), typeof(EnterprisePageLayout),
            new PropertyMetadata(null));

    /// <summary>Toolbar / filter bar slot (Row 1, Auto).</summary>
    public static readonly DependencyProperty ToolbarContentProperty =
        DependencyProperty.Register(
            nameof(ToolbarContent), typeof(object), typeof(EnterprisePageLayout),
            new PropertyMetadata(null));

    /// <summary>Bottom form slot for collapsible add/edit forms (Row 4, Auto).</summary>
    public static readonly DependencyProperty BottomFormContentProperty =
        DependencyProperty.Register(
            nameof(BottomFormContent), typeof(object), typeof(EnterprisePageLayout),
            new PropertyMetadata(null));

    /// <summary>Optional status bar slot (Row 5, Auto).</summary>
    public static readonly DependencyProperty StatusBarContentProperty =
        DependencyProperty.Register(
            nameof(StatusBarContent), typeof(object), typeof(EnterprisePageLayout),
            new PropertyMetadata(null));

    public object? TipBannerContent
    {
        get => GetValue(TipBannerContentProperty);
        set => SetValue(TipBannerContentProperty, value);
    }

    public object? ToolbarContent
    {
        get => GetValue(ToolbarContentProperty);
        set => SetValue(ToolbarContentProperty, value);
    }

    public object? BottomFormContent
    {
        get => GetValue(BottomFormContentProperty);
        set => SetValue(BottomFormContentProperty, value);
    }

    public object? StatusBarContent
    {
        get => GetValue(StatusBarContentProperty);
        set => SetValue(StatusBarContentProperty, value);
    }

    static EnterprisePageLayout()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(EnterprisePageLayout),
            new FrameworkPropertyMetadata(typeof(EnterprisePageLayout)));

        FocusableProperty.OverrideMetadata(
            typeof(EnterprisePageLayout),
            new FrameworkPropertyMetadata(false));
    }
}
