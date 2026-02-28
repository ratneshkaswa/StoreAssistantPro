using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that executes <see cref="FocusHint"/> transitions
/// produced by <see cref="IPredictiveFocusService"/> in the visual tree.
/// <para>
/// <b>This is the XAML-side executor.</b>  The service is the decision
/// engine (no WPF types); this behavior is the WPF bridge that actually
/// calls <c>UIElement.Focus()</c> or <c>MoveFocus()</c>.
/// </para>
///
/// <para><b>Focus resolution strategies:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Strategy</term>
///     <description>How it resolves</description>
///   </listheader>
///   <item>
///     <term><see cref="FocusStrategy.FirstInput"/></term>
///     <description><c>MoveFocus(FocusNavigationDirection.First)</c> inside
///     the target container — respects <c>TabIndex</c> ordering.</description>
///   </item>
///   <item>
///     <term><see cref="FocusStrategy.Named"/></term>
///     <description>Walks the visual tree to find an element whose
///     <c>Name</c> matches <see cref="FocusHint.ElementName"/>, then
///     calls <c>Focus()</c> on it.</description>
///   </item>
///   <item>
///     <term><see cref="FocusStrategy.Preserve"/></term>
///     <description>No-op — focus stays wherever it currently is.</description>
///   </item>
/// </list>
///
/// <para><b>Flicker prevention:</b></para>
/// <para>
/// All focus moves are dispatched at
/// <see cref="DispatcherPriority.Input"/> so the visual tree is fully
/// measured and arranged before focus is attempted.  This ensures:
/// <list type="bullet">
///   <item>Animated page transitions complete their first frame.</item>
///   <item>Data-bound content is populated (DataGrid rows, form fields).</item>
///   <item>Collapsible containers have their final Visibility.</item>
/// </list>
/// </para>
///
/// <para><b>Coordination with AutoFocus:</b></para>
/// <para>
/// When <see cref="IsEnabledProperty"/> is <c>True</c> on a Window, this
/// behavior handles focus on <c>Loaded</c> <em>and</em> on every subsequent
/// <see cref="FocusHintChangedEvent"/>.  The existing <c>AutoFocus.IsEnabled</c>
/// continues to work independently on elements that don't use this behavior.
/// </para>
///
/// <para><b>Usage (GlobalStyles.xaml — applied to all Windows):</b></para>
/// <code>
/// &lt;Style TargetType="Window"&gt;
///     &lt;Setter Property="h:PredictiveFocusBehavior.IsEnabled" Value="True"/&gt;
/// &lt;/Style&gt;
/// </code>
/// </summary>
public static class PredictiveFocusBehavior
{
    // ── IsEnabled DP ─────────────────────────────────────────────────

    /// <summary>
    /// Set to <c>True</c> on a <see cref="Window"/> or container to
    /// enable predictive focus execution. On <c>Loaded</c>, the behavior
    /// moves focus to the first input. Subsequently, it subscribes to
    /// <see cref="FocusHintChangedEvent"/> and executes each hint.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PredictiveFocusBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    // ── Private: event subscription stored on the element ────────────

    private static readonly DependencyProperty SubscriptionStateProperty =
        DependencyProperty.RegisterAttached(
            "SubscriptionState",
            typeof(HintSubscription),
            typeof(PredictiveFocusBehavior));

    // ── Activation / deactivation ────────────────────────────────────

    private static void OnIsEnabledChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        if (e.NewValue is true)
        {
            fe.Loaded += OnLoaded;
            fe.Unloaded += OnUnloaded;
            fe.PreviewMouseDown += OnPreviewMouseDown;
        }
        else
        {
            fe.Loaded -= OnLoaded;
            fe.Unloaded -= OnUnloaded;
            fe.PreviewMouseDown -= OnPreviewMouseDown;
            Detach(fe);
        }
    }

    // ── Mouse click → signal safety guard ────────────────────────────

    private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // If the user clicks on a focusable input, signal the safety
        // guard so programmatic hints are suppressed briefly.
        if (e.OriginalSource is not DependencyObject source)
            return;

        // Walk up to find the actual focusable control (TextBox, etc.)
        var focusable = FindFocusableAncestor(source);
        if (focusable is null)
            return;

        var guard = ResolveService<IFocusSafetyGuard>();
        guard?.SignalUserClick();
    }

    /// <summary>
    /// Walks up the visual tree to find the nearest focusable element
    /// (TextBox, ComboBox, PasswordBox, etc.).
    /// </summary>
    private static UIElement? FindFocusableAncestor(DependencyObject source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is UIElement ui && ui.Focusable)
                return ui;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    // ── Loaded: initial focus + subscribe ─────────────────────────────

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe)
            return;

        // Resolve the service from the application's DI container.
        // Fail silently at design time or if the service isn't registered.
        var eventBus = ResolveService<IEventBus>();
        if (eventBus is null)
            return;

        // Create subscription state and store on the element
        var sub = new HintSubscription(fe, eventBus);
        fe.SetValue(SubscriptionStateProperty, sub);
        sub.Attach();

        // ── Initial focus: dispatch at Input priority so the visual
        //    tree is fully laid out (avoids flicker on window open) ──
        fe.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            // If the service already has a pending hint (e.g., from a
            // navigation that happened before the window loaded), use it.
            var predictive = ResolveService<IPredictiveFocusService>();
            var pendingHint = predictive?.CurrentHint;

            if (pendingHint is not null && pendingHint.Strategy != FocusStrategy.Preserve)
            {
                ExecuteHint(fe, pendingHint);
            }
            else
            {
                // Default: move to first focusable input
                fe.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        });
    }

    // ── Unloaded: unsubscribe ────────────────────────────────────────

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
            Detach(fe);
    }

    private static void Detach(FrameworkElement fe)
    {
        var sub = fe.GetValue(SubscriptionStateProperty) as HintSubscription;
        sub?.Detach();
        fe.ClearValue(SubscriptionStateProperty);
    }

    // ── Hint execution ───────────────────────────────────────────────

    /// <summary>
    /// Executes a <see cref="FocusHint"/> against a target container.
    /// Dispatched at <see cref="DispatcherPriority.Input"/> to avoid flicker.
    /// </summary>
    internal static void ExecuteHint(FrameworkElement container, FocusHint hint)
    {
        switch (hint.Strategy)
        {
            case FocusStrategy.FirstInput:
                container.MoveFocus(
                    new TraversalRequest(FocusNavigationDirection.First));
                break;

            case FocusStrategy.Named:
                FocusNamedElement(container, hint.ElementName);
                break;

            case FocusStrategy.Preserve:
                // Intentional no-op
                break;
        }
    }

    /// <summary>
    /// Walks the visual tree from <paramref name="root"/> to find an element
    /// whose <c>Name</c> matches <paramref name="elementName"/>, then focuses it.
    /// Falls back to <c>MoveFocus(First)</c> if the named element isn't found.
    /// </summary>
    private static void FocusNamedElement(FrameworkElement root, string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
        {
            root.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            return;
        }

        // Try FrameworkElement.FindName first (fast, works for named
        // elements in the same namescope)
        if (root.FindName(elementName) is UIElement namedElement)
        {
            if (namedElement.Focusable)
            {
                namedElement.Focus();
            }
            else
            {
                // The named element isn't focusable itself (e.g., a Border
                // or Grid). Move focus to the first input inside it.
                namedElement.MoveFocus(
                    new TraversalRequest(FocusNavigationDirection.First));
            }
            return;
        }

        // Fallback: walk the visual tree for cross-namescope resolution
        var found = FindDescendantByName(root, elementName);
        if (found is not null)
        {
            if (found.Focusable)
            {
                found.Focus();
            }
            else
            {
                found.MoveFocus(
                    new TraversalRequest(FocusNavigationDirection.First));
            }
            return;
        }

        // Last resort: first focusable input in the container
        root.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    /// <summary>
    /// DFS walk of the visual tree looking for a <see cref="FrameworkElement"/>
    /// with the specified <c>Name</c>.
    /// </summary>
    private static FrameworkElement? FindDescendantByName(
        DependencyObject parent, string name)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe && fe.Name == name)
                return fe;

            var found = FindDescendantByName(child, name);
            if (found is not null)
                return found;
        }
        return null;
    }

    // ── Service resolution ───────────────────────────────────────────

    private static T? ResolveService<T>() where T : class
    {
        if (Application.Current is not App app)
            return null;

        return app.Services?.GetService(typeof(T)) as T;
    }

    // ── Subscription state ───────────────────────────────────────────

    /// <summary>
    /// Encapsulates the event subscription for a single element.
    /// Stored on the element via <see cref="SubscriptionStateProperty"/>
    /// so it can be detached on Unloaded.
    /// </summary>
    private sealed class HintSubscription
    {
        private readonly FrameworkElement _target;
        private readonly IEventBus _eventBus;
        private bool _attached;

        public HintSubscription(FrameworkElement target, IEventBus eventBus)
        {
            _target = target;
            _eventBus = eventBus;
        }

        public void Attach()
        {
            if (_attached) return;
            _eventBus.Subscribe<FocusHintChangedEvent>(OnHintChanged);
            _attached = true;
        }

        public void Detach()
        {
            if (!_attached) return;
            _eventBus.Unsubscribe<FocusHintChangedEvent>(OnHintChanged);
            _attached = false;
        }

        private Task OnHintChanged(FocusHintChangedEvent e)
        {
            // Dispatch on the UI thread at Input priority
            _target.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (!_target.IsLoaded)
                    return;

                // Safety guard: check all rules before executing
                var guard = ResolveService<IFocusSafetyGuard>();
                if (guard is not null && !guard.CanExecute(e.Hint))
                    return;

                ExecuteHint(_target, e.Hint);
            });
            return Task.CompletedTask;
        }
    }
}
