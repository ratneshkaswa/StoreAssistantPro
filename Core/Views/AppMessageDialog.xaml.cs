using System.Windows;
using System.Windows.Media;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Views;

public enum AppMessageDialogKind
{
    Question,
    Information
}

public partial class AppMessageDialog : BaseDialogWindow
{
    protected override double DialogWidth => 460;
    protected override double DialogHeight => 256;

    public AppMessageDialog(
        IWindowSizingService sizingService,
        string title,
        string message,
        AppMessageDialogKind kind,
        string primaryButtonText,
        string? secondaryButtonText = null)
        : base(sizingService)
    {
        InitializeComponent();

        Title = title;
        MessageText.Text = message;
        PrimaryButton.Content = primaryButtonText;

        if (string.IsNullOrWhiteSpace(secondaryButtonText))
        {
            SecondaryButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            SecondaryButton.Content = secondaryButtonText;
            SecondaryButton.Visibility = Visibility.Visible;
        }

        ApplyKind(kind);
        Loaded += OnLoaded;
    }

    public bool Confirmed { get; private set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (SecondaryButton.Visibility == Visibility.Visible)
        {
            SecondaryButton.IsDefault = true;
            SecondaryButton.Focus();
            return;
        }

        PrimaryButton.IsDefault = true;
        PrimaryButton.Focus();
    }

    private void ApplyKind(AppMessageDialogKind kind)
    {
        string glyph;
        Brush badgeBackground;
        Brush glyphForeground;

        switch (kind)
        {
            case AppMessageDialogKind.Information:
                glyph = "i";
                badgeBackground = (Brush)FindResource("FluentInfoBackground");
                glyphForeground = (Brush)FindResource("FluentInfo");
                break;
            default:
                glyph = "?";
                badgeBackground = (Brush)FindResource("FluentInfoBackground");
                glyphForeground = (Brush)FindResource("FluentInfo");
                break;
        }

        IconGlyph.Text = glyph;
        IconBadge.Background = badgeBackground;
        IconGlyph.Foreground = glyphForeground;
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
    }

    private void OnSecondaryClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
