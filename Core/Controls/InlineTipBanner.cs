using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Compact inline banner that displays a guidance tip inside a window.
/// <para>
/// <b>Visual layout:</b>
/// <code>
/// ┌─────────────────────────────────────────────────────┐
/// │ ▎ 💡  Title                                    [✕]  │
/// │ ▎     Tip text goes here.                           │
/// └─────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// <para>
/// The banner uses dedicated design-system tokens from
/// <c>DesignSystem.xaml</c>:
/// </para>
/// <list type="table">
///   <listheader><term>Token</term><description>Role</description></listheader>
///   <item><term>TipBannerBackground</term><description>Near-white surface (#F8F9FB).</description></item>
///   <item><term>TipBannerBackgroundHover</term><description>Subtle hover tint (#F2F4F8).</description></item>
///   <item><term>TipBannerBorder</term><description>Neutral-cool stroke (#E6E8ED).</description></item>
///   <item><term>TipBannerAccent</term><description>Muted left bar (#B4D6F5).</description></item>
///   <item><term>TipBannerIconForeground</term><description>Accent icon (#0078D4).</description></item>
///   <item><term>TipBannerTitleForeground</term><description>Near-black title (#242424).</description></item>
///   <item><term>TipBannerBodyForeground</term><description>Medium-grey body (#707070).</description></item>
/// </list>
/// <para>
/// Uses <c>FluentCornerMedium</c> (8 px) rounded corners and
/// compact height suited for toolbar or form-area placement.
/// The close button is hidden at rest and fades in on banner
/// hover — keeping the visual weight minimal.
/// </para>
///
/// <para><b>Dismiss behavior:</b></para>
/// <list type="number">
///   <item>User clicks the close button (or ViewModel sets
///         <see cref="IsDismissed"/> = <c>true</c>).</item>
///   <item>A two-phase animation plays: opacity fade-out followed
///         by a height collapse, preventing a jarring layout jump.</item>
///   <item>After animation completes:
///     <list type="bullet">
///       <item><see cref="IsDismissed"/> is set to <c>true</c>
///             (two-way binding pushes to the ViewModel).</item>
///       <item><see cref="DismissCommand"/> is executed if bound.</item>
///       <item>The <see cref="DismissedEvent"/> routed event is raised
///             so parent views can react in XAML.</item>
///     </list>
///   </item>
///   <item>Setting <see cref="IsDismissed"/> back to <c>false</c>
///         restores visibility and resets animations — no reload
///         needed.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;controls:InlineTipBanner
///     Title="Quick tip"
///     TipText="Use Ctrl+N to start a new sale from any screen."
///     IsDismissed="{Binding IsSalesTipDismissed, Mode=TwoWay}"/&gt;
/// </code>
/// </summary>
public class InlineTipBanner : Control
{
    // ── Routed event: Dismissed ────────────────────────────────────

    /// <summary>
    /// Raised after the dismiss animation completes and the banner
    /// collapses. Bubbles up the visual tree so ancestor panels can
    /// react (e.g., play a complementary layout animation).
    /// </summary>
    public static readonly RoutedEvent DismissedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(Dismissed),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(InlineTipBanner));

    /// <summary>Occurs after the banner is dismissed.</summary>
    public event RoutedEventHandler Dismissed
    {
        add => AddHandler(DismissedEvent, value);
        remove => RemoveHandler(DismissedEvent, value);
    }

    // ── Title DP ──────────────────────────────────────────────────

    /// <summary>
    /// Bold title text displayed after the bulb icon.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(InlineTipBanner),
            new PropertyMetadata("Tip"));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // ── TipText DP ────────────────────────────────────────────────

    /// <summary>
    /// Descriptive tip text displayed after the title.
    /// </summary>
    public static readonly DependencyProperty TipTextProperty =
        DependencyProperty.Register(
            nameof(TipText), typeof(string), typeof(InlineTipBanner),
            new PropertyMetadata(string.Empty));

    public string TipText
    {
        get => (string)GetValue(TipTextProperty);
        set => SetValue(TipTextProperty, value);
    }

    // ── IsDismissed DP ────────────────────────────────────────────

    /// <summary>
    /// When <c>true</c> the banner is collapsed. Supports two-way
    /// binding so the ViewModel can persist dismiss state.
    /// <para>
    /// Setting to <c>true</c> from code or a ViewModel binding
    /// triggers the same animated dismiss sequence as clicking the
    /// close button.  Setting back to <c>false</c> restores the
    /// banner with full opacity — no window reload needed.
    /// </para>
    /// </summary>
    public static readonly DependencyProperty IsDismissedProperty =
        DependencyProperty.Register(
            nameof(IsDismissed), typeof(bool), typeof(InlineTipBanner),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsDismissedChanged));

    public bool IsDismissed
    {
        get => (bool)GetValue(IsDismissedProperty);
        set => SetValue(IsDismissedProperty, value);
    }

    // ── DismissCommand DP ─────────────────────────────────────────

    /// <summary>
    /// Optional command executed after the dismiss animation completes.
    /// Receives no parameter. Use to persist the dismissed state in a
    /// settings store or analytics service.
    /// </summary>
    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(
            nameof(DismissCommand), typeof(ICommand), typeof(InlineTipBanner));

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    // ── Template parts ────────────────────────────────────────────

    private const string PartRoot = "PART_Root";
    private const string PartCloseButton = "PART_CloseButton";

    private Border? _root;
    private Button? _closeButton;

    // ── Animation state ───────────────────────────────────────────

    private bool _isDismissing;

    // ── Constructor ───────────────────────────────────────────────

    static InlineTipBanner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(InlineTipBanner),
            new FrameworkPropertyMetadata(typeof(InlineTipBanner)));
    }

    // ── Template wiring ───────────────────────────────────────────

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Detach from previous template parts
        if (_closeButton is not null)
        {
            _closeButton.Click -= OnCloseButtonClick;
            _closeButton = null;
        }
        _root = null;

        _root = GetTemplateChild(PartRoot) as Border;
        _closeButton = GetTemplateChild(PartCloseButton) as Button;

        if (_closeButton is not null)
            _closeButton.Click += OnCloseButtonClick;

        // Honour pre-set dismissed state (e.g. bound before template applied)
        if (IsDismissed)
        {
            Visibility = Visibility.Collapsed;
            if (_root is not null)
                _root.Opacity = 0;
        }
    }

    // ── Close logic ───────────────────────────────────────────────

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        AnimateDismiss();
    }

    /// <summary>
    /// Plays a two-phase dismiss animation (fade-out → height collapse)
    /// then finalises the dismissed state.  Safe to call multiple times
    /// — re-entrant calls are ignored while an animation is in flight.
    /// </summary>
    private void AnimateDismiss()
    {
        if (_isDismissing)
            return;

        if (_root is null)
        {
            FinaliseDismiss();
            return;
        }

        _isDismissing = true;

        // Capture the current rendered height for the collapse animation
        var currentHeight = _root.ActualHeight;

        // Phase 1: Fade out
        var fadeOut = new DoubleAnimation(1, 0, ResolveDuration(fast: false))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (_, _) =>
        {
            // Phase 2: Collapse height to zero (prevents layout jump)
            if (currentHeight > 0)
            {
                var collapse = new DoubleAnimation(currentHeight, 0, ResolveDuration(fast: true))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                collapse.Completed += (_, _) =>
                {
                    // Clear the animated height so the DP returns to Auto
                    _root.BeginAnimation(HeightProperty, null);
                    FinaliseDismiss();
                };

                _root.BeginAnimation(HeightProperty, collapse);
            }
            else
            {
                FinaliseDismiss();
            }
        };

        _root.BeginAnimation(OpacityProperty, fadeOut);
    }

    /// <summary>
    /// Completes the dismiss: collapses visibility, updates the DP,
    /// executes the command, raises the routed event.
    /// </summary>
    private void FinaliseDismiss()
    {
        Visibility = Visibility.Collapsed;

        // Set the DP without re-triggering AnimateDismiss
        _isDismissing = true;
        IsDismissed = true;
        _isDismissing = false;

        if (DismissCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);

        RaiseEvent(new RoutedEventArgs(DismissedEvent, this));
    }

    /// <summary>
    /// Resolves animation duration from the resource tree.
    /// <paramref name="fast"/> selects FluentDurationFast (83 ms)
    /// for the height collapse phase; otherwise FluentDurationNormal
    /// (167 ms) for the opacity fade.
    /// </summary>
    private Duration ResolveDuration(bool fast)
    {
        var key = fast ? "FluentDurationFast" : "FluentDurationNormal";
        if (TryFindResource(key) is Duration d)
            return d;
        return new Duration(TimeSpan.FromMilliseconds(fast ? 83 : 167));
    }

    // ── DP changed callback ───────────────────────────────────────

    private static void OnIsDismissedChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not InlineTipBanner banner)
            return;

        if (e.NewValue is true)
        {
            // Animate when set by ViewModel binding (skip if already animating)
            if (!banner._isDismissing)
                banner.AnimateDismiss();
        }
        else
        {
            // Restore: reset all animated properties and show
            banner._isDismissing = false;

            if (banner._root is not null)
            {
                banner._root.BeginAnimation(OpacityProperty, null);
                banner._root.BeginAnimation(HeightProperty, null);
                banner._root.Opacity = 1;
            }

            banner.Visibility = Visibility.Visible;
        }
    }
}
