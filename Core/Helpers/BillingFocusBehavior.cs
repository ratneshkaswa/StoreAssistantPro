using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that traps keyboard focus within a designated
/// billing workspace when activated.
/// <para>
/// <b>Focus trapping rules:</b>
/// <list type="bullet">
///   <item>Tab / Shift+Tab cycle within the target container — focus
///         wraps around instead of escaping.</item>
///   <item>Escape is swallowed so it cannot dismiss billing mode.</item>
///   <item>If focus leaves the container (e.g., via mouse click on a
///         dimmed panel), focus is immediately pulled back to the first
///         focusable control inside the container.</item>
///   <item>Elements marked with <see cref="AllowFocusProperty"/> remain
///         reachable (e.g., the billing mode toggle button).</item>
/// </list>
/// </para>
///
/// <para><b>Usage (MainWindow.xaml):</b></para>
/// <code>
/// &lt;controls:ResponsiveContentControl
///     h:BillingFocusBehavior.IsActive="{Binding FocusLock.IsFocusLocked}"
///     Content="{Binding CurrentView}"/&gt;
/// </code>
///
/// <para><b>Exempt an external control:</b></para>
/// <code>
/// &lt;Button h:BillingFocusBehavior.AllowFocus="True"
///         Command="{Binding ToggleBillingModeCommand}"/&gt;
/// </code>
///
/// <para>
/// The behavior is stateless — attaching and detaching is driven
/// entirely by the <see cref="IsActiveProperty"/> value. The hosting
/// <c>Window</c> is resolved lazily on first activation to listen
/// for <c>PreviewGotKeyboardFocus</c>.
/// </para>
/// </summary>
public static class BillingFocusBehavior
{
    // ── IsActive attached property ────────────────────────────────

    /// <summary>
    /// Set to <c>True</c> to trap keyboard focus inside the target element.
    /// Bind to <c>FocusLock.IsFocusLocked</c>.
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.RegisterAttached(
            "IsActive",
            typeof(bool),
            typeof(BillingFocusBehavior),
            new PropertyMetadata(false, OnIsActiveChanged));

    public static bool GetIsActive(DependencyObject obj) =>
        (bool)obj.GetValue(IsActiveProperty);

    public static void SetIsActive(DependencyObject obj, bool value) =>
        obj.SetValue(IsActiveProperty, value);

    // ── AllowFocus attached property ──────────────────────────────

    /// <summary>
    /// Set to <c>True</c> on elements outside the focus-trapped region
    /// that must remain keyboard-accessible (e.g., the billing mode
    /// toggle button).
    /// </summary>
    public static readonly DependencyProperty AllowFocusProperty =
        DependencyProperty.RegisterAttached(
            "AllowFocus",
            typeof(bool),
            typeof(BillingFocusBehavior),
            new PropertyMetadata(false));

    public static bool GetAllowFocus(DependencyObject obj) =>
        (bool)obj.GetValue(AllowFocusProperty);

    public static void SetAllowFocus(DependencyObject obj, bool value) =>
        obj.SetValue(AllowFocusProperty, value);

    // ── Private state stored on the element ───────────────────────

    private static readonly DependencyProperty FocusTrapHandlerProperty =
        DependencyProperty.RegisterAttached(
            "FocusTrapHandler",
            typeof(FocusTrapState),
            typeof(BillingFocusBehavior));

    // ── Activation / deactivation ─────────────────────────────────

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement target)
            return;

        if ((bool)e.NewValue)
            Activate(target);
        else
            Deactivate(target);
    }

    private static void Activate(FrameworkElement target)
    {
        var state = new FocusTrapState(target);
        target.SetValue(FocusTrapHandlerProperty, state);

        target.PreviewKeyDown += state.OnPreviewKeyDown;

        var window = Window.GetWindow(target);
        if (window is not null)
        {
            state.Window = window;
            window.PreviewGotKeyboardFocus += state.OnWindowPreviewGotKeyboardFocus;
        }

        // Pull focus into the billing area immediately
        target.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            () => EnsureFocusInside(target));
    }

    private static void Deactivate(FrameworkElement target)
    {
        var state = target.GetValue(FocusTrapHandlerProperty) as FocusTrapState;
        if (state is null)
            return;

        target.PreviewKeyDown -= state.OnPreviewKeyDown;

        if (state.Window is not null)
            state.Window.PreviewGotKeyboardFocus -= state.OnWindowPreviewGotKeyboardFocus;

        target.ClearValue(FocusTrapHandlerProperty);
    }

    // ── Focus helpers ─────────────────────────────────────────────

    /// <summary>
    /// Moves keyboard focus to the first focusable control inside
    /// <paramref name="container"/> if focus is not already there.
    /// </summary>
    internal static void EnsureFocusInside(UIElement container)
    {
        var focused = Keyboard.FocusedElement as DependencyObject;
        if (focused is not null && IsDescendant(container, focused))
            return;

        container.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="candidate"/> is
    /// <paramref name="ancestor"/> itself or is inside its visual tree.
    /// </summary>
    internal static bool IsDescendant(DependencyObject ancestor, DependencyObject candidate)
    {
        var current = candidate;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    /// <summary>
    /// Walks up the visual tree from <paramref name="element"/> looking
    /// for any ancestor (or self) with <see cref="AllowFocusProperty"/>
    /// set to <c>true</c>.
    /// </summary>
    internal static bool HasAllowFocusAncestor(DependencyObject element)
    {
        var current = element;
        while (current is not null)
        {
            if (current.ReadLocalValue(AllowFocusProperty) is bool allow && allow)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    // ── Focus trap state (per-element) ────────────────────────────

    /// <summary>
    /// Holds the event handlers for a single trapped element so they
    /// can be cleanly unsubscribed on deactivation.
    /// </summary>
    private sealed class FocusTrapState(FrameworkElement target)
    {
        public Window? Window { get; set; }

        /// <summary>
        /// Handles Tab wrap-around and Escape suppression inside
        /// the billing workspace.
        /// </summary>
        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                // ── Escape: swallow so it cannot leave billing ──
                case Key.Escape:
                    e.Handled = true;
                    break;

                // ── Tab: wrap focus within container ──
                case Key.Tab:
                    HandleTabWrap(e);
                    break;
            }
        }

        /// <summary>
        /// If focus is about to land outside the billing workspace,
        /// redirect it back inside — unless the target element is
        /// marked with <see cref="AllowFocusProperty"/>.
        /// </summary>
        public void OnWindowPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus is not DependencyObject newTarget)
                return;

            // Allow focus within the billing workspace
            if (IsDescendant(target, newTarget))
                return;

            // Allow focus on exempt elements (e.g., mode toggle button)
            if (HasAllowFocusAncestor(newTarget))
                return;

            // Redirect focus back inside
            e.Handled = true;
            target.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Input,
                () => EnsureFocusInside(target));
        }

        private void HandleTabWrap(KeyEventArgs e)
        {
            var direction = (Keyboard.Modifiers & ModifierKeys.Shift) != 0
                ? FocusNavigationDirection.Previous
                : FocusNavigationDirection.Next;

            var focused = Keyboard.FocusedElement as UIElement;
            if (focused is null)
                return;

            // Predict where Tab would go
            var prediction = focused.PredictFocus(direction) as DependencyObject;

            // If the prediction is null (end of tab sequence) or would
            // leave the container, wrap to the opposite end.
            if (prediction is null || !IsDescendant(target, prediction))
            {
                e.Handled = true;
                var wrapDirection = direction == FocusNavigationDirection.Next
                    ? FocusNavigationDirection.First
                    : FocusNavigationDirection.Last;

                target.MoveFocus(new TraversalRequest(wrapDirection));
            }
            // Otherwise, let the default Tab behavior proceed within the container.
        }
    }
}
