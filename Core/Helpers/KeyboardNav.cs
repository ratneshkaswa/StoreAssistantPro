using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that provides enterprise keyboard navigation:
/// <list type="bullet">
///   <item><b>Enter</b> — moves focus to the next editable input control,
///         skipping disabled and read-only controls.  When no editable
///         control follows, executes <see cref="DefaultCommandProperty"/>
///         if bound and <c>CanExecute</c> is true.</item>
///   <item><b>Shift+Enter</b> — moves focus to the previous editable
///         input control.</item>
///   <item><b>Escape</b> — executes <see cref="EscapeCommandProperty"/> if
///         bound on the nearest ancestor; otherwise clears focus from the
///         current input control; otherwise lets the event bubble so that
///         WPF <c>IsCancel</c> buttons still work.</item>
/// </list>
/// <para>
/// The behavior is applied to a container (Window, UserControl, Grid, etc.)
/// and automatically handles all focusable input descendants.
/// Tab order follows the standard WPF <c>TabIndex</c> / visual-tree order.
/// </para>
///
/// <para><b>Global activation (GlobalStyles.xaml):</b></para>
/// <code>
/// &lt;Style TargetType="Window"&gt;
///     &lt;Setter Property="h:KeyboardNav.IsEnabled" Value="True"/&gt;
/// &lt;/Style&gt;
/// </code>
///
/// <para><b>Enter → submit form after last field (bind in XAML):</b></para>
/// <code>
/// &lt;Grid h:KeyboardNav.DefaultCommand="{Binding SaveCommand}"&gt;
///     &lt;TextBox Text="{Binding Name}"/&gt;       &lt;!-- Enter → next --&gt;
///     &lt;TextBox Text="{Binding Price}"/&gt;      &lt;!-- Enter → submit --&gt;
///     &lt;Button Content="Save" Command="{Binding SaveCommand}"/&gt;
/// &lt;/Grid&gt;
/// </code>
///
/// <para><b>ESC → close dialog (BaseDialogWindow sets this automatically):</b></para>
/// <code>
/// KeyboardNav.SetEscapeCommand(this, myCloseCommand);
/// </code>
///
/// <para><b>ESC → cancel form (bind in XAML):</b></para>
/// <code>
/// &lt;Border h:KeyboardNav.EscapeCommand="{Binding CancelFormCommand}"&gt;
///     &lt;!-- form fields --&gt;
/// &lt;/Border&gt;
/// </code>
///
/// <para>
/// Both <c>DefaultCommand</c> and <c>EscapeCommand</c> are resolved by
/// walking up the visual tree from the focused element.  The nearest
/// ancestor with the command set wins, so an inner form can override the
/// outer container's behavior.
/// </para>
/// </summary>
public static class KeyboardNav
{
    // ── IsEnabled ────────────────────────────────────────────────────

    /// <summary>
    /// Set to <c>True</c> on any container to enable Enter/Escape key
    /// navigation for all input descendants.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(KeyboardNav),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    // ── DefaultCommand ───────────────────────────────────────────────

    /// <summary>
    /// Primary-action command executed when Enter is pressed on an input
    /// control inside this container and <c>CanExecute</c> returns true.
    /// <para>
    /// Resolved by walking up the visual tree from the focused element —
    /// the nearest ancestor with a non-null command wins.  This lets an
    /// inner form card override a window-level default.
    /// </para>
    /// <para>
    /// When the command's <c>CanExecute</c> returns <c>false</c>, Enter
    /// falls back to moving focus to the next control (standard
    /// navigation).  This prevents accidental submits — bind the same
    /// command used on the primary button and its validation guards both.
    /// </para>
    /// </summary>
    public static readonly DependencyProperty DefaultCommandProperty =
        DependencyProperty.RegisterAttached(
            "DefaultCommand",
            typeof(ICommand),
            typeof(KeyboardNav),
            new PropertyMetadata(null));

    public static ICommand? GetDefaultCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(DefaultCommandProperty);

    public static void SetDefaultCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(DefaultCommandProperty, value);

    // ── DefaultCommandParameter ──────────────────────────────────────

    /// <summary>
    /// Optional parameter passed to <see cref="DefaultCommandProperty"/>.
    /// Must be set on the same element as the command.
    /// </summary>
    public static readonly DependencyProperty DefaultCommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "DefaultCommandParameter",
            typeof(object),
            typeof(KeyboardNav),
            new PropertyMetadata(null));

    public static object? GetDefaultCommandParameter(DependencyObject obj) =>
        obj.GetValue(DefaultCommandParameterProperty);

    public static void SetDefaultCommandParameter(DependencyObject obj, object? value) =>
        obj.SetValue(DefaultCommandParameterProperty, value);

    // ── EscapeCommand ────────────────────────────────────────────────

    /// <summary>
    /// Command executed when ESC is pressed inside this container.
    /// The nearest ancestor with a non-null command wins (visual-tree walk).
    /// <para>
    /// When no command is bound, ESC clears focus if an input control is
    /// focused; otherwise the event is not handled, allowing WPF
    /// <c>IsCancel</c> buttons to function normally.
    /// </para>
    /// </summary>
    public static readonly DependencyProperty EscapeCommandProperty =
        DependencyProperty.RegisterAttached(
            "EscapeCommand",
            typeof(ICommand),
            typeof(KeyboardNav),
            new PropertyMetadata(null));

    public static ICommand? GetEscapeCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(EscapeCommandProperty);

    public static void SetEscapeCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(EscapeCommandProperty, value);

    // ── EscapeCommandParameter ───────────────────────────────────────

    /// <summary>
    /// Optional parameter passed to <see cref="EscapeCommandProperty"/>.
    /// Must be set on the same element as the command.
    /// </summary>
    public static readonly DependencyProperty EscapeCommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "EscapeCommandParameter",
            typeof(object),
            typeof(KeyboardNav),
            new PropertyMetadata(null));

    public static object? GetEscapeCommandParameter(DependencyObject obj) =>
        obj.GetValue(EscapeCommandParameterProperty);

    public static void SetEscapeCommandParameter(DependencyObject obj, object? value) =>
        obj.SetValue(EscapeCommandParameterProperty, value);

    // ── Wiring ───────────────────────────────────────────────────────

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        if ((bool)e.NewValue)
            element.PreviewKeyDown += OnPreviewKeyDown;
        else
            element.PreviewKeyDown -= OnPreviewKeyDown;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                HandleEnter(e);
                break;
            case Key.Escape:
                HandleEscape(e);
                break;
        }
    }

    // ── Enter handling ───────────────────────────────────────────────

    private static void HandleEnter(KeyEventArgs e)
    {
        if (ShouldIgnoreEnter(e.OriginalSource as DependencyObject))
            return;

        var source = e.OriginalSource as UIElement;
        var direction = (Keyboard.Modifiers & ModifierKeys.Shift) != 0
            ? FocusNavigationDirection.Previous
            : FocusNavigationDirection.Next;

        // Shift+Enter always navigates backward — never submits.
        if (direction == FocusNavigationDirection.Previous)
        {
            e.Handled = true;
            MoveToNextEditable(source, direction);
            return;
        }

        // Forward: try to move to the next editable input control.
        // If one exists, go there.  If not, execute DefaultCommand.
        if (MoveToNextEditable(source, FocusNavigationDirection.Next))
        {
            e.Handled = true;
            return;
        }

        // No editable control ahead → try DefaultCommand (submit).
        var commandOwner = FindNearestWithProperty(
            e.OriginalSource as DependencyObject, DefaultCommandProperty);
        if (commandOwner is not null)
        {
            var command = GetDefaultCommand(commandOwner)!;
            var parameter = GetDefaultCommandParameter(commandOwner);
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
                e.Handled = true;
                return;
            }
        }

        // Nothing to submit — just move focus (lets WPF pick the next
        // focusable element, which may be a button).
        e.Handled = true;
        source?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
    }

    /// <summary>
    /// Returns <c>true</c> for controls where Enter has a native meaning
    /// that should not be overridden.
    /// </summary>
    private static bool ShouldIgnoreEnter(DependencyObject? source) => source switch
    {
        TextBox tb when tb.AcceptsReturn => true,
        ButtonBase => true,
        ComboBox cb when cb.IsDropDownOpen => true,
        _ => false
    };

    /// <summary>
    /// Moves focus to the next (or previous) editable input control,
    /// skipping disabled, read-only, and non-input elements.
    /// Returns <c>true</c> if an editable control was found and focused.
    /// </summary>
    private static bool MoveToNextEditable(UIElement? source, FocusNavigationDirection direction)
    {
        if (source is null)
            return false;

        // Use WPF's built-in focus prediction to find the next candidate
        // without actually moving focus.
        var request = new TraversalRequest(direction);
        var current = source;

        // Walk through up to 20 candidates to find an editable input.
        // The cap prevents infinite loops in degenerate visual trees.
        for (var i = 0; i < 20; i++)
        {
            var next = current.PredictFocus(direction) as UIElement;
            if (next is null || next == source)
                return false;

            if (IsEditableInput(next))
            {
                next.Focus();
                return true;
            }

            current = next;
        }

        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the element is an editable input control
    /// that should participate in Enter-key field navigation.
    /// </summary>
    private static bool IsEditableInput(UIElement element) => element switch
    {
        TextBox tb => tb.IsEnabled && !tb.IsReadOnly && tb.Visibility == Visibility.Visible,
        PasswordBox pb => pb.IsEnabled && pb.Visibility == Visibility.Visible,
        ComboBox cb => cb.IsEnabled && cb.Visibility == Visibility.Visible,
        DatePicker dp => dp.IsEnabled && dp.Visibility == Visibility.Visible,
        _ => false
    };

    // ── Escape handling ──────────────────────────────────────────────

    private static void HandleEscape(KeyEventArgs e)
    {
        // Priority 1: Execute the nearest ancestor's EscapeCommand.
        // This supports dialog-close, form-cancel, and nested overrides.
        var commandOwner = FindNearestWithProperty(
            e.OriginalSource as DependencyObject, EscapeCommandProperty);
        if (commandOwner is not null)
        {
            var command = GetEscapeCommand(commandOwner)!;
            var parameter = GetEscapeCommandParameter(commandOwner);
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
                e.Handled = true;
                return;
            }
        }

        // Priority 2: Clear focus if an input control is focused.
        if (e.OriginalSource is TextBox or PasswordBox)
        {
            Keyboard.ClearFocus();
            e.Handled = true;
            return;
        }

        // Priority 3: Don't handle — let IsCancel buttons close the window.
    }

    // ── Shared tree-walk ─────────────────────────────────────────────

    /// <summary>
    /// Walks up the visual tree from <paramref name="source"/> and returns
    /// the first ancestor that has the specified attached property set to
    /// a non-null value.
    /// </summary>
    private static DependencyObject? FindNearestWithProperty(
        DependencyObject? source, DependencyProperty property)
    {
        var current = source;
        while (current is not null)
        {
            if (current.GetValue(property) is not null)
                return current;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
