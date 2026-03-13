using System.Linq;
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
    protected override double DialogWidth => 420;
    protected override double DialogHeight => 180;
    protected override double DialogMinWidth => 360;
    protected override double DialogMinHeight => 0;

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
        SizeToContent = SizeToContent.Height;
        MinHeight = 0;

        Title = string.IsNullOrWhiteSpace(title) ? "Message" : title;
        DialogTitleText.Text = Title;
        MessageText.Text = message;
        MessageText.Visibility = string.IsNullOrWhiteSpace(message)
            ? Visibility.Collapsed
            : Visibility.Visible;
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
        UpdateDialogWidth();

        if (SecondaryButton.Visibility == Visibility.Visible)
        {
            SecondaryButton.IsCancel = true;
            SecondaryButton.IsDefault = true;
            SecondaryButton.Focus();
            return;
        }

        PrimaryButton.IsDefault = true;
        PrimaryButton.Focus();
    }

    private void UpdateDialogWidth()
    {
        var longestButtonText = new[]
        {
            PrimaryButton.Content?.ToString(),
            SecondaryButton.Visibility == Visibility.Visible ? SecondaryButton.Content?.ToString() : null
        }
        .Where(text => !string.IsNullOrWhiteSpace(text))
        .MaxBy(text => text!.Length);

        var messageLength = MessageText.Text?.Length ?? 0;
        var hasLongContent = messageLength > 90 || (longestButtonText?.Length ?? 0) > 10;

        Width = hasLongContent ? 460 : 420;
        MinWidth = 360;
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
                badgeBackground = (Brush)FindResource("FluentWarningBackground");
                glyphForeground = (Brush)FindResource("FluentWarning");
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
