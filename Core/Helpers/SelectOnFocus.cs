using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that provides smart cursor positioning when a
/// <see cref="TextBox"/> receives keyboard focus:
/// <list type="bullet">
///   <item><b>Numeric fields</b> (<c>NumericInput.IsIntegerOnly</c> or
///         <c>IsDecimalOnly</c>) — cursor moves to end of text, ready
///         for appending or immediate overwrite.</item>
///   <item><b>Text fields</b> — all text is selected for fast overwrite.</item>
/// </list>
/// <para>
/// Multi-line <c>TextBox</c> controls (<c>AcceptsReturn="True"</c>) are
/// automatically excluded — selecting all text on every focus would
/// disrupt editing in note / description fields.
/// </para>
/// <para>
/// Numeric vs. text is auto-detected from the existing
/// <see cref="NumericInput"/> attached properties — no extra attributes
/// needed on any XAML control.
/// </para>
///
/// <para><b>Global activation (GlobalStyles.xaml — implicit TextBox style):</b></para>
/// <code>
/// &lt;Style TargetType="TextBox"&gt;
///     &lt;Setter Property="h:SelectOnFocus.IsEnabled" Value="True"/&gt;
/// &lt;/Style&gt;
/// </code>
///
/// <para><b>Opt-out on a specific control:</b></para>
/// <code>
/// &lt;TextBox h:SelectOnFocus.IsEnabled="False" AcceptsReturn="True" .../&gt;
/// </code>
/// </summary>
public static class SelectOnFocus
{
    /// <summary>
    /// Set to <c>True</c> on a <see cref="TextBox"/> to enable smart
    /// cursor positioning when the control receives keyboard focus.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SelectOnFocus),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        if ((bool)e.NewValue)
        {
            textBox.GotKeyboardFocus += OnGotKeyboardFocus;
            textBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        }
        else
        {
            textBox.GotKeyboardFocus -= OnGotKeyboardFocus;
            textBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        }
    }

    /// <summary>
    /// Positions the cursor when the TextBox gains keyboard focus (Tab,
    /// Enter navigation, or programmatic focus).
    /// <list type="bullet">
    ///   <item>Numeric fields → cursor at end of text.</item>
    ///   <item>Text fields → select all text.</item>
    /// </list>
    /// Skips multi-line controls (<c>AcceptsReturn</c>).
    /// </summary>
    private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox { AcceptsReturn: false } textBox)
            return;

        if (IsNumericField(textBox))
            textBox.CaretIndex = textBox.Text.Length;
        else
            textBox.SelectAll();
    }

    /// <summary>
    /// Handles the mouse click case.  Without this, a mouse click sets
    /// the caret position <i>after</i> GotKeyboardFocus fires, which
    /// would undo the positioning immediately.  By capturing focus in
    /// Preview and marking the event handled, we ensure the cursor
    /// position set in <see cref="OnGotKeyboardFocus"/> sticks.
    /// <para>
    /// If the TextBox already has focus (user clicking to reposition the
    /// caret), the event is not handled — normal caret behavior applies.
    /// </para>
    /// </summary>
    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBox { AcceptsReturn: false, IsKeyboardFocusWithin: false } textBox)
            return;

        // Force focus so GotKeyboardFocus fires with the smart positioning,
        // then mark handled to prevent the click from repositioning the caret.
        textBox.Focus();
        e.Handled = true;
    }

    /// <summary>
    /// Returns <c>true</c> if the TextBox has a <see cref="NumericInput"/>
    /// attached property set.  Auto-detected — no extra XAML needed.
    /// </summary>
    private static bool IsNumericField(TextBox textBox) =>
        NumericInput.GetIsIntegerOnly(textBox) || NumericInput.GetIsDecimalOnly(textBox);
}
