using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Displays the app logo (from Assets/logo-64.png) + styled "Store Assistant Pro" title.
/// Falls back to a 🏪 emoji if the logo image file is not found.
/// </summary>
public class AppBranding : Control
{
    public static readonly DependencyProperty LogoSizeProperty =
        DependencyProperty.Register(nameof(LogoSize), typeof(double), typeof(AppBranding),
            new PropertyMetadata(48.0));

    public static readonly DependencyProperty ShowTitleProperty =
        DependencyProperty.Register(nameof(ShowTitle), typeof(bool), typeof(AppBranding),
            new PropertyMetadata(true));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(AppBranding),
            new PropertyMetadata(string.Empty));

    public double LogoSize
    {
        get => (double)GetValue(LogoSizeProperty);
        set => SetValue(LogoSizeProperty, value);
    }

    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Returns true if the logo-64.png resource exists.</summary>
    public static bool HasLogoAsset
    {
        get
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/logo-64.png", UriKind.Absolute);
                var info = Application.GetResourceStream(uri);
                return info != null;
            }
            catch
            {
                return false;
            }
        }
    }

    static AppBranding()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AppBranding),
            new FrameworkPropertyMetadata(typeof(AppBranding)));
    }
}
