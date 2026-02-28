using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that automatically sets keyboard focus to the first
/// focusable input control when a container is loaded.
/// <para>
/// Focus target is resolved using <see cref="FocusNavigationDirection.First"/>,
/// which respects <c>TabIndex</c> ordering.  If no focusable control exists
/// inside the container, the call is a harmless no-op.
/// </para>
///
/// <para><b>Global activation (GlobalStyles.xaml):</b></para>
/// <code>
/// &lt;Style TargetType="Window"&gt;
///     &lt;Setter Property="h:AutoFocus.IsEnabled" Value="True"/&gt;
/// &lt;/Style&gt;
/// </code>
///
/// <para><b>Per-container usage:</b></para>
/// <code>
/// &lt;Grid h:AutoFocus.IsEnabled="True"&gt;
///     &lt;TextBox .../&gt;   &lt;!-- receives focus on load --&gt;
/// &lt;/Grid&gt;
/// </code>
///
/// <para><b>Opt-out:</b></para>
/// <code>
/// &lt;Window h:AutoFocus.IsEnabled="False"&gt;
/// </code>
/// </summary>
public static class AutoFocus
{
    /// <summary>
    /// Set to <c>True</c> on any <see cref="FrameworkElement"/> container
    /// to focus the first input control when the element is loaded.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(AutoFocus),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
            element.Loaded += OnLoaded;
        else
            element.Loaded -= OnLoaded;
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not UIElement container)
            return;

        // MoveFocus with First respects TabIndex ordering and finds the
        // first focusable control in the visual subtree.  If nothing is
        // focusable the call does nothing.
        container.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
