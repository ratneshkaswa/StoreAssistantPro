using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Semi-transparent overlay with shimmering skeleton placeholders and a
/// supporting <see cref="ProgressRing"/> that covers its parent when
/// <see cref="IsActive"/> is <c>true</c>.
/// <para>
/// Place as the last child inside a <see cref="Grid"/> so it layers on top.
/// </para>
/// <example>
/// <code>
/// &lt;Grid&gt;
///     &lt;!-- Normal content --&gt;
///     &lt;controls:LoadingOverlay IsActive="{Binding IsLoading}"/&gt;
/// &lt;/Grid&gt;
/// </code>
/// </example>
/// </summary>
public class LoadingOverlay : Control
{
    static LoadingOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(LoadingOverlay), new FrameworkPropertyMetadata(typeof(LoadingOverlay)));
    }

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(LoadingOverlay),
            new PropertyMetadata(false));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay),
            new PropertyMetadata("Preparing content"));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
}
