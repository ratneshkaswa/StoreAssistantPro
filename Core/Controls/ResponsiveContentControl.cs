using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// A <see cref="ContentControl"/> designed for the MainWindow content region
/// that stretches its content to fill available space while enabling vertical
/// scrolling when the content's desired height exceeds the viewport.
/// <para>
/// The default style (defined in <c>App.xaml</c>) wraps the content presenter
/// inside a <see cref="ScrollViewer"/> and a <see cref="ViewportConstrainedPanel"/>
/// that passes the viewport size as the measure constraint — preserving
/// star-sized Grid rows inside hosted views.
/// </para>
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;controls:ResponsiveContentControl Content="{Binding CurrentView}"/&gt;
/// </code>
/// </summary>
public class ResponsiveContentControl : ContentControl
{
    static ResponsiveContentControl()
    {
        FocusableProperty.OverrideMetadata(
            typeof(ResponsiveContentControl),
            new FrameworkPropertyMetadata(false));
    }
}
