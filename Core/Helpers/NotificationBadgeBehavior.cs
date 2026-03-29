using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that manages a notification count badge on any
/// <see cref="Panel"/>. The badge is a visual overlay positioned at
/// the top-right corner.
/// <para>
/// <b>Auto-update:</b> Bind <see cref="CountProperty"/> to
/// <c>AppState.UnreadNotificationCount</c>. The badge automatically
/// collapses when count reaches zero and reappears when count &gt; 0.
/// </para>
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;Grid h:NotificationBadgeBehavior.Count="{Binding AppState.UnreadNotificationCount}"&gt;
///     &lt;TextBlock Text="🔔" FontSize="18"/&gt;
/// &lt;/Grid&gt;
/// </code>
/// </summary>
public static class NotificationBadgeBehavior
{
    // ── Count attached property ───────────────────────────────────

    /// <summary>
    /// The unread notification count. When &gt; 0 the badge is shown;
    /// when 0 it collapses.
    /// </summary>
    public static readonly DependencyProperty CountProperty =
        DependencyProperty.RegisterAttached(
            "Count",
            typeof(int),
            typeof(NotificationBadgeBehavior),
            new PropertyMetadata(0, OnCountChanged));

    public static int GetCount(DependencyObject obj) =>
        (int)obj.GetValue(CountProperty);

    public static void SetCount(DependencyObject obj, int value) =>
        obj.SetValue(CountProperty, value);

    // ── BadgeBackground attached property ─────────────────────────

    /// <summary>
    /// Background brush for the badge circle. Defaults to red (#E53935).
    /// </summary>
    public static readonly DependencyProperty BadgeBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "BadgeBackground",
            typeof(Brush),
            typeof(NotificationBadgeBehavior),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)), OnAppearanceChanged));

    public static Brush GetBadgeBackground(DependencyObject obj) =>
        (Brush)obj.GetValue(BadgeBackgroundProperty);

    public static void SetBadgeBackground(DependencyObject obj, Brush value) =>
        obj.SetValue(BadgeBackgroundProperty, value);

    // ── BadgeForeground attached property ─────────────────────────

    /// <summary>
    /// Foreground brush for the badge count text. Defaults to White.
    /// </summary>
    public static readonly DependencyProperty BadgeForegroundProperty =
        DependencyProperty.RegisterAttached(
            "BadgeForeground",
            typeof(Brush),
            typeof(NotificationBadgeBehavior),
            new PropertyMetadata(Brushes.White, OnAppearanceChanged));

    public static Brush GetBadgeForeground(DependencyObject obj) =>
        (Brush)obj.GetValue(BadgeForegroundProperty);

    public static void SetBadgeForeground(DependencyObject obj, Brush value) =>
        obj.SetValue(BadgeForegroundProperty, value);

    // ── DotOnly attached property ─────────────────────────────────

    /// <summary>
    /// When true, renders the badge as a compact 8px notification dot
    /// instead of showing the numeric unread count.
    /// </summary>
    public static readonly DependencyProperty DotOnlyProperty =
        DependencyProperty.RegisterAttached(
            "DotOnly",
            typeof(bool),
            typeof(NotificationBadgeBehavior),
            new PropertyMetadata(false, OnAppearanceChanged));

    public static bool GetDotOnly(DependencyObject obj) =>
        (bool)obj.GetValue(DotOnlyProperty);

    public static void SetDotOnly(DependencyObject obj, bool value) =>
        obj.SetValue(DotOnlyProperty, value);

    // ── Private: badge element stored on the target ───────────────

    private static readonly DependencyProperty BadgeElementProperty =
        DependencyProperty.RegisterAttached(
            "BadgeElement",
            typeof(Border),
            typeof(NotificationBadgeBehavior));

    // ── Change handlers ───────────────────────────────────────────

    private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel)
            return;

        var newCount = (int)e.NewValue;
        var badge = GetOrCreateBadge(panel);

        UpdateBadge(badge, newCount, panel);
    }

    private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel)
            return;

        var badge = (Border?)panel.GetValue(BadgeElementProperty);
        if (badge is null)
            return;

        UpdateBadge(badge, GetCount(panel), panel);
    }

    // ── Badge creation ────────────────────────────────────────────

    private static Border GetOrCreateBadge(Panel panel)
    {
        var existing = (Border?)panel.GetValue(BadgeElementProperty);
        if (existing is not null)
            return existing;

        var textBlock = new TextBlock
        {
            FontSize = GetMinimumTextFontSize(panel),
            FontWeight = FontWeights.Bold,
            Foreground = GetBadgeForeground(panel),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true
        };
        TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.ClearType);

        var badge = new Border
        {
            Background = GetBadgeBackground(panel),
            CornerRadius = new CornerRadius(8),
            MinWidth = 16,
            Height = 16,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -4, -4, 0),
            Padding = new Thickness(4, 0, 4, 0),
            IsHitTestVisible = false,
            Child = textBlock,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true
        };
        TextOptions.SetTextFormattingMode(badge, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(badge, TextRenderingMode.ClearType);
        ApplyBadgeLayout(badge, panel);

        panel.Children.Add(badge);
        panel.SetValue(BadgeElementProperty, badge);

        return badge;
    }

    // ── Badge update ──────────────────────────────────────────────

    private static void UpdateBadge(Border badge, int count, Panel panel)
    {
        ApplyBadgeLayout(badge, panel);

        if (badge.Child is TextBlock tb)
        {
            tb.Foreground = GetBadgeForeground(panel);
            if (GetDotOnly(panel))
            {
                tb.Text = string.Empty;
                tb.Visibility = Visibility.Collapsed;
            }
            else
            {
                tb.Text = count > 99 ? "99+" : count.ToString();
                tb.Visibility = Visibility.Visible;
            }
        }

        badge.Background = GetBadgeBackground(panel);
        badge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void ApplyBadgeLayout(Border badge, Panel panel)
    {
        var dotOnly = GetDotOnly(panel);

        if (dotOnly)
        {
            badge.CornerRadius = new CornerRadius(4);
            badge.Width = 8;
            badge.MinWidth = 8;
            badge.Height = 8;
            badge.Padding = new Thickness(0);
            badge.Margin = new Thickness(0, -2, -2, 0);
        }
        else
        {
            badge.ClearValue(FrameworkElement.WidthProperty);
            badge.CornerRadius = new CornerRadius(10);
            badge.MinWidth = 20;
            badge.Height = 20;
            badge.Padding = new Thickness(5, 0, 5, 0);
            badge.Margin = new Thickness(0, -4, -4, 0);
        }
    }

    private static double GetMinimumTextFontSize(FrameworkElement element)
    {
        if (element.TryFindResource("FontSizeLabel") is double fontSize)
            return fontSize;

        return 13;
    }

}
