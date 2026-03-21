using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Centralized smart tooltip service exposed as attached properties.
/// Manages tooltip timing globally to prevent spam while supporting
/// rich content and Fluent-style presentation.
///
/// <para><b>Global timing model:</b></para>
/// <list type="bullet">
///   <item><b>Cold delay</b> (500 ms) — informational tips appear with
///         a Win11-style hover delay that is quick without feeling
///         noisy. Configurable via <c>TooltipDelayColdMs</c>
///         resource token.</item>
///   <item><b>Warm delay</b> (500 ms) — subsequent hovers within the
///         warm window keep the same predictable delay instead of
///         accelerating further. Configurable via
///         <c>TooltipDelayWarmMs</c> token.</item>
///   <item><b>Warm window</b> (1.5 s) — after hiding a tooltip the
///         system stays "warm" so the next hover opens faster.
///         Configurable via <c>TooltipWarmWindowMs</c> token.</item>
///   <item><b>Display duration</b> (5 s) — auto-hide after this
///         interval unless the mouse remains over the element.
///         Configurable via <c>TooltipDisplayMs</c> token.</item>
/// </list>
///
/// <para><b>Anti-flicker guarantees:</b></para>
/// <list type="bullet">
///   <item>Show timer checks <c>IsMouseOver</c> before opening —
///         prevents ghost tooltip when mouse left during delay.</item>
///   <item>Active element identity verified on every timer tick —
///         prevents stale state from rapid enter/leave cycles.</item>
///   <item>Warm state only set when a tooltip was <em>actually
///         visible</em> — prevents false warm on cancelled hovers.</item>
///   <item>Per-element bounce guard via monotonic generation counter —
///         ensures only the latest hover intent opens a tooltip.</item>
/// </list>
///
/// <para><b>Available attached properties:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Property</term>
///     <description>Purpose</description>
///   </listheader>
///   <item>
///     <term><see cref="TextProperty"/></term>
///     <description>Simple text tooltip (most common usage).</description>
///   </item>
///   <item>
///     <term><see cref="HeaderProperty"/></term>
///     <description>Bold header line above the body text.</description>
///   </item>
///   <item>
///     <term><see cref="ShortcutProperty"/></term>
///     <description>Keyboard shortcut displayed in secondary text below the description.</description>
///   </item>
///   <item>
///     <term><see cref="UsageTipProperty"/></term>
///     <description>Brief usage tip displayed in caption style at the bottom of the tooltip.</description>
///   </item>
///   <item>
///     <term><see cref="ContentProperty"/></term>
///     <description>Rich content (any <see cref="object"/>). When set,
///     <c>Text</c> is ignored and this is placed directly into the
///     tooltip's <c>ContentPresenter</c>.</description>
///   </item>
///   <item>
///     <term><see cref="PlacementProperty"/></term>
///     <description>Override the default <c>Bottom</c> placement.</description>
///   </item>
///   <item>
///     <term><see cref="IsEnabledProperty"/></term>
///     <description>Dynamically enable/disable the tooltip.</description>
///   </item>
///   <item>
///     <term><see cref="ContextKeyProperty"/></term>
///     <description>Help key for <see cref="Services.IContextHelpService"/>
///     integration. When set, tooltip text and usage tip are resolved
///     from the context rule pipeline at hover time.</description>
///   </item>
/// </list>
///
/// <para><b>Usage (XAML):</b></para>
/// <code>
/// &lt;!-- Simple text --&gt;
/// &lt;Button h:SmartTooltip.Text="Save changes"  … /&gt;
///
/// &lt;!-- Header + text --&gt;
/// &lt;Button h:SmartTooltip.Header="Save"
///         h:SmartTooltip.Text="Save all pending changes to the database" … /&gt;
///
/// &lt;!-- Header + text + shortcut + usage tip --&gt;
/// &lt;Button h:SmartTooltip.Header="Save"
///         h:SmartTooltip.Text="Save all pending changes to the database"
///         h:SmartTooltip.Shortcut="Ctrl+S"
///         h:SmartTooltip.UsageTip="Works only when changes are pending" … /&gt;
///
/// &lt;!-- Rich content --&gt;
/// &lt;Border h:SmartTooltip.Content="{StaticResource MyRichTooltipPanel}" … /&gt;
///
/// &lt;!-- Conditional --&gt;
/// &lt;Button h:SmartTooltip.Text="Disabled in read-only mode"
///         h:SmartTooltip.IsEnabled="{Binding IsReadOnly}" … /&gt;
/// </code>
/// </summary>
public static class SmartTooltip
{
    // ═════════════════════════════════════════════════════════════════
    //  Global on/off switch  (set from System Settings)
    // ═════════════════════════════════════════════════════════════════

    private static bool _globalEnabled = true;

    /// <summary>
    /// Master switch for the entire tooltip system.
    /// When set to <c>false</c>, no tooltips will appear and any
    /// currently visible tooltip is dismissed immediately.
    /// Designed to be toggled from System Settings at runtime.
    /// </summary>
    public static bool GlobalEnabled
    {
        get => _globalEnabled;
        set
        {
            if (_globalEnabled == value)
                return;

            _globalEnabled = value;

            if (!value)
                DismissCurrent();
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Global timing state  (singleton, lives for app lifetime)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>Last time a <em>visible</em> tooltip was hidden.</summary>
    private static DateTime _lastHideTime = DateTime.MinValue;

    /// <summary>Whether the last dismiss was of a tooltip that was actually shown.</summary>
    private static bool _lastDismissWasVisible;

    /// <summary>Element whose tooltip is currently visible (or pending).</summary>
    private static FrameworkElement? _activeElement;

    /// <summary>Timer that opens the tooltip after the appropriate delay.</summary>
    private static DispatcherTimer? _showTimer;

    /// <summary>Timer that auto-hides the tooltip after display duration.</summary>
    private static DispatcherTimer? _hideTimer;

    /// <summary>
    /// Monotonic generation counter. Incremented on every mouse-enter
    /// and captured into the timer closure — if the generation has
    /// advanced by the time the timer fires, the hover intent is stale.
    /// </summary>
    private static long _generation;

    // Compile-time fallbacks (overridden at runtime from DesignSystem tokens)
    private const double FallbackColdDelayMs = 500;
    private const double FallbackWarmDelayMs = 500;
    private const double FallbackWarmWindowMs = 1500;
    private const double FallbackDisplayMs = 5000;

    // ═════════════════════════════════════════════════════════════════
    //  TEXT  —  simple string tooltip
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(SmartTooltip),
            new PropertyMetadata(null, OnTooltipPropertyChanged));

    public static string? GetText(DependencyObject obj) =>
        (string?)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string? value) =>
        obj.SetValue(TextProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  HEADER  —  bold line above text
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.RegisterAttached(
            "Header", typeof(string), typeof(SmartTooltip),
            new PropertyMetadata(null, OnTooltipPropertyChanged));

    public static string? GetHeader(DependencyObject obj) =>
        (string?)obj.GetValue(HeaderProperty);

    public static void SetHeader(DependencyObject obj, string? value) =>
        obj.SetValue(HeaderProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  SHORTCUT  —  keyboard shortcut line (secondary text)
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty ShortcutProperty =
        DependencyProperty.RegisterAttached(
            "Shortcut", typeof(string), typeof(SmartTooltip),
            new PropertyMetadata(null, OnTooltipPropertyChanged));

    public static string? GetShortcut(DependencyObject obj) =>
        (string?)obj.GetValue(ShortcutProperty);

    public static void SetShortcut(DependencyObject obj, string? value) =>
        obj.SetValue(ShortcutProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  USAGE TIP  —  brief contextual tip (caption style)
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty UsageTipProperty =
        DependencyProperty.RegisterAttached(
            "UsageTip", typeof(string), typeof(SmartTooltip),
            new PropertyMetadata(null, OnTooltipPropertyChanged));

    public static string? GetUsageTip(DependencyObject obj) =>
        (string?)obj.GetValue(UsageTipProperty);

    public static void SetUsageTip(DependencyObject obj, string? value) =>
        obj.SetValue(UsageTipProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  CONTENT  —  rich/arbitrary content (overrides Text)
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.RegisterAttached(
            "Content", typeof(object), typeof(SmartTooltip),
            new PropertyMetadata(null, OnTooltipPropertyChanged));

    public static object? GetContent(DependencyObject obj) =>
        obj.GetValue(ContentProperty);

    public static void SetContent(DependencyObject obj, object? value) =>
        obj.SetValue(ContentProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  PLACEMENT  —  override Popup placement
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty PlacementProperty =
        DependencyProperty.RegisterAttached(
            "Placement", typeof(PlacementMode), typeof(SmartTooltip),
            new PropertyMetadata(PlacementMode.Bottom));

    public static PlacementMode GetPlacement(DependencyObject obj) =>
        (PlacementMode)obj.GetValue(PlacementProperty);

    public static void SetPlacement(DependencyObject obj, PlacementMode value) =>
        obj.SetValue(PlacementProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  IS ENABLED  —  dynamic toggle
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(SmartTooltip),
            new PropertyMetadata(true, OnTooltipPropertyChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  CONTEXT KEY  —  ContextHelpService integration
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Help key used to resolve context-aware text from
    /// <see cref="ContextResolver"/> at hover time. When set,
    /// <c>BuildTooltip</c> calls the resolver and merges the
    /// returned <see cref="Services.ContextHelpResult"/> into the
    /// tooltip — overriding <c>Text</c> and enriching <c>UsageTip</c>.
    /// </summary>
    public static readonly DependencyProperty ContextKeyProperty =
        DependencyProperty.RegisterAttached(
            "ContextKey", typeof(string), typeof(SmartTooltip),
            new PropertyMetadata(null, OnTooltipPropertyChanged));

    public static string? GetContextKey(DependencyObject obj) =>
        (string?)obj.GetValue(ContextKeyProperty);

    public static void SetContextKey(DependencyObject obj, string? value) =>
        obj.SetValue(ContextKeyProperty, value);

    /// <summary>
    /// Delegate signature for context-aware help resolution.
    /// Accepts a help key and returns a resolved result, or <c>null</c>
    /// when no rule matches.
    /// </summary>
    public delegate ContextHelpResult? ContextResolveFunc(string key);

    /// <summary>
    /// Global context resolver set once at app startup by
    /// <see cref="ContextHelpService"/>. <c>null</c> when
    /// no resolver is registered (e.g. in unit tests or design mode).
    /// <para>
    /// Using a static delegate avoids coupling the static
    /// <c>SmartTooltip</c> class to the DI container.
    /// </para>
    /// </summary>
    public static ContextResolveFunc? ContextResolver { get; set; }

    // ═════════════════════════════════════════════════════════════════
    //  SUBSCRIBED  —  internal flag to avoid double-subscribing events
    // ═════════════════════════════════════════════════════════════════

    private static readonly DependencyProperty SubscribedProperty =
        DependencyProperty.RegisterAttached(
            "Subscribed", typeof(bool), typeof(SmartTooltip),
            new PropertyMetadata(false));

    // ═════════════════════════════════════════════════════════════════
    //  Wiring
    // ═════════════════════════════════════════════════════════════════

    private static void OnTooltipPropertyChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        // Check if there's any tooltip content configured
        var hasContent = GetText(fe) is not null
                      || GetHeader(fe) is not null
                      || GetShortcut(fe) is not null
                      || GetUsageTip(fe) is not null
                      || GetContent(fe) is not null
                      || GetContextKey(fe) is not null;

        var isSubscribed = (bool)fe.GetValue(SubscribedProperty);

        if (hasContent && GetIsEnabled(fe) && !isSubscribed)
        {
            // Disable WPF's built-in ToolTipService to prevent double tooltips
            ToolTipService.SetIsEnabled(fe, false);

            fe.MouseEnter += OnMouseEnter;
            fe.MouseLeave += OnMouseLeave;
            fe.Unloaded += OnUnloaded;
            fe.SetValue(SubscribedProperty, true);
        }
        else if ((!hasContent || !GetIsEnabled(fe)) && isSubscribed)
        {
            fe.MouseEnter -= OnMouseEnter;
            fe.MouseLeave -= OnMouseLeave;
            fe.Unloaded -= OnUnloaded;
            fe.SetValue(SubscribedProperty, false);
            ToolTipService.SetIsEnabled(fe, true);

            // If this element is active, dismiss immediately
            if (ReferenceEquals(_activeElement, fe))
                DismissCurrent();
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Mouse event handlers
    // ═════════════════════════════════════════════════════════════════

    private static void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_globalEnabled)
            return;

        if (sender is not FrameworkElement fe)
            return;

        // Dismiss any existing tooltip from a different element
        if (_activeElement is not null && !ReferenceEquals(_activeElement, fe))
            DismissCurrent();

        _activeElement = fe;

        // Capture a new generation so stale timer ticks are discarded
        var gen = ++_generation;

        // Determine delay: warm (slightly faster) vs cold (full delay)
        var warmWindow = ResolveMs(fe, "TooltipWarmWindowMs", FallbackWarmWindowMs);
        var isWarm = _lastDismissWasVisible
                  && (DateTime.UtcNow - _lastHideTime) < TimeSpan.FromMilliseconds(warmWindow);

        var delayMs = isWarm
            ? ResolveMs(fe, "TooltipDelayWarmMs", FallbackWarmDelayMs)
            : ResolveMs(fe, "TooltipDelayColdMs", FallbackColdDelayMs);

        StopTimers();

        _showTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(delayMs)
        };
        _showTimer.Tick += (_, _) => OnShowTimerTick(fe, gen);
        _showTimer.Start();
    }

    private static void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not FrameworkElement fe)
            return;

        // Only dismiss if this is the active element
        if (ReferenceEquals(_activeElement, fe))
            DismissCurrent();
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe)
            return;

        if (ReferenceEquals(_activeElement, fe))
            DismissCurrent();

        // Clean up event handlers
        fe.MouseEnter -= OnMouseEnter;
        fe.MouseLeave -= OnMouseLeave;
        fe.Unloaded -= OnUnloaded;
        fe.SetValue(SubscribedProperty, false);
    }

    // ═════════════════════════════════════════════════════════════════
    //  Show / Hide logic
    // ═════════════════════════════════════════════════════════════════

    private static void OnShowTimerTick(FrameworkElement target, long expectedGen)
    {
        _showTimer?.Stop();

        // ── Anti-flicker guard 0: global switch ─────────────────
        if (!_globalEnabled)
            return;

        // ── Anti-flicker guard 1: generation check ──────────────
        // If another mouse-enter has occurred since this timer was
        // started, the generation will have advanced — bail out.
        if (expectedGen != _generation)
            return;

        // ── Anti-flicker guard 2: identity check ────────────────
        // Ensure the element is still the active one (could have
        // been cleared by a rapid leave → enter on another element).
        if (!ReferenceEquals(_activeElement, target))
            return;

        // ── Anti-flicker guard 3: hit-test check ────────────────
        // The mouse may have left the element between the timer
        // start and this tick.  Re-verify before showing.
        if (!target.IsMouseOver)
        {
            _activeElement = null;
            return;
        }

        ShowTooltip(target);

        // Start auto-hide timer
        var displayMs = ResolveMs(target, "TooltipDisplayMs", FallbackDisplayMs);
        _hideTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(displayMs)
        };
        _hideTimer.Tick += OnHideTimerTick;
        _hideTimer.Start();
    }

    private static void OnHideTimerTick(object? sender, EventArgs e)
    {
        _hideTimer?.Stop();

        if (_activeElement is null)
            return;

        // Only auto-hide if the mouse is no longer over the element
        if (!_activeElement.IsMouseOver)
        {
            DismissCurrent();
        }
        else
        {
            // Mouse is still over — restart the hide timer
            _hideTimer?.Start();
        }
    }

    private static void ShowTooltip(FrameworkElement fe)
    {
        var tooltip = BuildTooltip(fe);
        if (tooltip is null)
            return;

        tooltip.PlacementTarget = fe;
        tooltip.Placement = GetPlacement(fe);
        tooltip.IsOpen = true;

        SetActiveTooltip(fe, tooltip);
    }

    private static void DismissCurrent()
    {
        StopTimers();

        var wasVisible = false;

        if (_activeElement is not null)
        {
            var existing = GetActiveTooltip(_activeElement);
            if (existing is not null)
            {
                wasVisible = existing.IsOpen;
                existing.IsOpen = false;
                SetActiveTooltip(_activeElement, null);
            }
        }

        // Only enter warm state when a tooltip was actually visible;
        // cancelled hover-delays should not pollute the warm window.
        if (wasVisible)
        {
            _lastHideTime = DateTime.UtcNow;
            _lastDismissWasVisible = true;
        }
        else
        {
            _lastDismissWasVisible = false;
        }

        _activeElement = null;
    }

    private static void StopTimers()
    {
        if (_showTimer is not null)
        {
            _showTimer.Stop();
            _showTimer = null;
        }

        if (_hideTimer is not null)
        {
            _hideTimer.Stop();
            _hideTimer.Tick -= OnHideTimerTick;
            _hideTimer = null;
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Tooltip construction
    // ═════════════════════════════════════════════════════════════════

    private static ToolTip? BuildTooltip(FrameworkElement fe)
    {
        var richContent = GetContent(fe);
        var header = GetHeader(fe);
        var text = GetText(fe);
        var shortcut = GetShortcut(fe);
        var usageTip = GetUsageTip(fe);

        // ── Context-aware resolution ────────────────────────────
        // When a ContextKey is set and a resolver is registered,
        // resolve at hover time so the tooltip always reflects the
        // current app state (mode, offline, focus lock).
        var contextKey = GetContextKey(fe);
        if (contextKey is not null && ContextResolver is not null)
        {
            var result = ContextResolver(contextKey);
            if (result is not null)
            {
                if (result.EffectiveDescription is not null)
                    text = result.EffectiveDescription;

                if (result.UsageTip is not null)
                    usageTip = result.UsageTip;
            }
        }

        if (richContent is null && text is null && header is null
            && shortcut is null && usageTip is null)
            return null;

        var tooltip = new ToolTip
        {
            UseLayoutRounding = true,
            SnapsToDevicePixels = true
        };
        ApplyCrispTextRendering(tooltip);

        // Try to apply the Fluent style from the resource tree
        if (fe.TryFindResource("SmartTooltipStyle") is Style tooltipStyle)
            tooltip.Style = tooltipStyle;

        if (richContent is not null)
        {
            // Rich content mode — place directly
            tooltip.Content = richContent;
        }
        else
        {
            tooltip.Content = BuildTooltipContent(fe, header, text, shortcut, usageTip);
        }

        return tooltip;
    }

    /// <summary>
    /// Builds the structured tooltip content panel.
    /// Layout: Title (bold) → Description → Shortcut (secondary) → Usage tip (caption).
    /// </summary>
    private static StackPanel BuildTooltipContent(
        FrameworkElement fe, string? header, string? text,
        string? shortcut, string? usageTip)
    {
        var panel = new StackPanel
        {
            UseLayoutRounding = true,
            SnapsToDevicePixels = true
        };
        ApplyCrispTextRendering(panel);
        var hasBody = text is not null || shortcut is not null || usageTip is not null;

        // ── Title (bold) ────────────────────────────────────────
        if (header is not null)
        {
            var headerBlock = new TextBlock
            {
                Text = header,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.WrapWithOverflow,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
            };
            ApplyCrispTextRendering(headerBlock);

            if (fe.TryFindResource("FontSizeBody") is double bodySize)
                headerBlock.FontSize = bodySize;

            if (fe.TryFindResource("FluentTextPrimary") is System.Windows.Media.Brush headerBrush)
                headerBlock.Foreground = headerBrush;

            if (hasBody)
                headerBlock.Margin = new Thickness(0, 0, 0, 3);

            panel.Children.Add(headerBlock);
        }

        // ── Description ─────────────────────────────────────────
        if (text is not null)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.WrapWithOverflow,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
            };
            ApplyCrispTextRendering(textBlock);

            if (fe.TryFindResource("TooltipMaxWidth") is double maxW)
                textBlock.MaxWidth = maxW - 24; // account for padding

            if (fe.TryFindResource("FontSizeLabel") is double labelSize)
                textBlock.FontSize = labelSize;

            if (fe.TryFindResource("FluentTextSecondary") is System.Windows.Media.Brush textBrush)
                textBlock.Foreground = textBrush;

            // Single-line text without header uses primary color
            // for better readability at the compact size.
            if (header is null && fe.TryFindResource("FluentTextPrimary") is System.Windows.Media.Brush primaryBrush)
                textBlock.Foreground = primaryBrush;

            panel.Children.Add(textBlock);
        }

        // ── Shortcut (secondary text) ───────────────────────────
        if (shortcut is not null)
        {
            var shortcutBlock = new TextBlock
            {
                Text = shortcut,
                TextWrapping = TextWrapping.NoWrap,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
            };
            ApplyCrispTextRendering(shortcutBlock);

            if (fe.TryFindResource("FontSizeLabel") is double scLabelSize)
                shortcutBlock.FontSize = scLabelSize;

            if (fe.TryFindResource("FluentTextSecondary") is System.Windows.Media.Brush scBrush)
                shortcutBlock.Foreground = scBrush;

            shortcutBlock.Margin = new Thickness(0, 3, 0, 0);

            panel.Children.Add(shortcutBlock);
        }

        // ── Usage tip (caption) ─────────────────────────────────
        if (usageTip is not null)
        {
            var tipBlock = new TextBlock
            {
                Text = usageTip,
                TextWrapping = TextWrapping.WrapWithOverflow,
                FontStyle = FontStyles.Italic,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
            };
            ApplyCrispTextRendering(tipBlock);

            if (fe.TryFindResource("FontSizeCaption") is double captionSize)
                tipBlock.FontSize = captionSize;

            if (fe.TryFindResource("FluentTextTertiary") is System.Windows.Media.Brush tipBrush)
                tipBlock.Foreground = tipBrush;

            tipBlock.Margin = new Thickness(0, 3, 0, 0);

            panel.Children.Add(tipBlock);
        }

        return panel;
    }

    // ═════════════════════════════════════════════════════════════════
    //  Per-element active tooltip tracking
    // ═════════════════════════════════════════════════════════════════

    private static readonly DependencyProperty ActiveTooltipProperty =
        DependencyProperty.RegisterAttached(
            "ActiveTooltip", typeof(ToolTip), typeof(SmartTooltip),
            new PropertyMetadata(null));

    private static ToolTip? GetActiveTooltip(DependencyObject obj) =>
        (ToolTip?)obj.GetValue(ActiveTooltipProperty);

    private static void SetActiveTooltip(DependencyObject obj, ToolTip? value) =>
        obj.SetValue(ActiveTooltipProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  Token resolution
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reads a <c>sys:Double</c> resource from <c>DesignSystem.xaml</c>.
    /// Falls back to compile-time constant if the token is missing
    /// (e.g. in unit-test hosts without a resource tree).
    /// </summary>
    private static double ResolveMs(FrameworkElement fe, string key, double fallback) =>
        fe.TryFindResource(key) is double v ? v : fallback;

    private static void ApplyCrispTextRendering(DependencyObject element)
    {
        TextOptions.SetTextFormattingMode(element, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(element, TextRenderingMode.ClearType);
    }
}

