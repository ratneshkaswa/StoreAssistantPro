using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Adds Win11-style press-and-hold reveal behavior to <see cref="PasswordBox"/>.
/// The plaintext is only exposed through the attached <c>RevealText</c> property
/// while reveal is actively pressed.
/// </summary>
public static class PasswordReveal
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PasswordReveal),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty RevealTextProperty =
        DependencyProperty.RegisterAttached(
            "RevealText",
            typeof(string),
            typeof(PasswordReveal),
            new PropertyMetadata(string.Empty));

    public static string GetRevealText(DependencyObject obj) =>
        (string)obj.GetValue(RevealTextProperty);

    private static void SetRevealText(DependencyObject obj, string value) =>
        obj.SetValue(RevealTextProperty, value);

    public static readonly DependencyProperty HasRevealTextProperty =
        DependencyProperty.RegisterAttached(
            "HasRevealText",
            typeof(bool),
            typeof(PasswordReveal),
            new PropertyMetadata(false));

    public static bool GetHasRevealText(DependencyObject obj) =>
        (bool)obj.GetValue(HasRevealTextProperty);

    private static void SetHasRevealText(DependencyObject obj, bool value) =>
        obj.SetValue(HasRevealTextProperty, value);

    public static readonly DependencyProperty IsRevealActiveProperty =
        DependencyProperty.RegisterAttached(
            "IsRevealActive",
            typeof(bool),
            typeof(PasswordReveal),
            new PropertyMetadata(false, OnIsRevealActiveChanged));

    public static bool GetIsRevealActive(DependencyObject obj) =>
        (bool)obj.GetValue(IsRevealActiveProperty);

    public static void SetIsRevealActive(DependencyObject obj, bool value) =>
        obj.SetValue(IsRevealActiveProperty, value);

    private static readonly DependencyProperty IsSubscribedProperty =
        DependencyProperty.RegisterAttached(
            "IsSubscribed",
            typeof(bool),
            typeof(PasswordReveal),
            new PropertyMetadata(false));

    private static readonly DependencyProperty RevealButtonProperty =
        DependencyProperty.RegisterAttached(
            "RevealButton",
            typeof(ButtonBase),
            typeof(PasswordReveal),
            new PropertyMetadata(null));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
            return;

        if ((bool)e.NewValue)
            Attach(passwordBox);
        else
            Detach(passwordBox);
    }

    private static void OnIsRevealActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
            return;

        if ((bool)e.NewValue && GetIsEnabled(passwordBox) && !string.IsNullOrEmpty(passwordBox.Password))
            SetRevealText(passwordBox, passwordBox.Password);
        else
            SetRevealText(passwordBox, string.Empty);
    }

    private static void Attach(PasswordBox passwordBox)
    {
        if ((bool)passwordBox.GetValue(IsSubscribedProperty))
        {
            AttachRevealButton(passwordBox);
            SyncState(passwordBox);
            return;
        }

        passwordBox.PasswordChanged += OnPasswordChanged;
        passwordBox.Loaded += OnPasswordBoxLoaded;
        passwordBox.Unloaded += OnPasswordBoxUnloaded;
        passwordBox.SetValue(IsSubscribedProperty, true);

        AttachRevealButton(passwordBox);
        SyncState(passwordBox);
    }

    private static void Detach(PasswordBox passwordBox)
    {
        if (!(bool)passwordBox.GetValue(IsSubscribedProperty))
            return;

        passwordBox.PasswordChanged -= OnPasswordChanged;
        passwordBox.Loaded -= OnPasswordBoxLoaded;
        passwordBox.Unloaded -= OnPasswordBoxUnloaded;
        passwordBox.SetValue(IsSubscribedProperty, false);

        DetachRevealButton(passwordBox);
        SetIsRevealActive(passwordBox, false);
        SetHasRevealText(passwordBox, false);
        SetRevealText(passwordBox, string.Empty);
    }

    private static void OnPasswordBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
            return;

        AttachRevealButton(passwordBox);
        SyncState(passwordBox);
    }

    private static void OnPasswordBoxUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
            return;

        SetIsRevealActive(passwordBox, false);
        DetachRevealButton(passwordBox);
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
            return;

        SyncState(passwordBox);
    }

    private static void SyncState(PasswordBox passwordBox)
    {
        var hasText = !string.IsNullOrEmpty(passwordBox.Password);
        SetHasRevealText(passwordBox, hasText);

        if (GetIsRevealActive(passwordBox) && hasText)
            SetRevealText(passwordBox, passwordBox.Password);
        else
            SetRevealText(passwordBox, string.Empty);
    }

    private static void AttachRevealButton(PasswordBox passwordBox)
    {
        if (passwordBox.Template?.FindName("PART_RevealButton", passwordBox) is not ButtonBase button)
            return;

        if (ReferenceEquals(passwordBox.GetValue(RevealButtonProperty), button))
            return;

        DetachRevealButton(passwordBox);

        button.PreviewMouseLeftButtonDown += OnRevealButtonMouseDown;
        button.PreviewMouseLeftButtonUp += OnRevealButtonMouseUp;
        button.MouseLeave += OnRevealButtonMouseLeave;
        button.LostMouseCapture += OnRevealButtonLostMouseCapture;

        passwordBox.SetValue(RevealButtonProperty, button);
    }

    private static void DetachRevealButton(PasswordBox passwordBox)
    {
        if (passwordBox.GetValue(RevealButtonProperty) is not ButtonBase button)
            return;

        button.PreviewMouseLeftButtonDown -= OnRevealButtonMouseDown;
        button.PreviewMouseLeftButtonUp -= OnRevealButtonMouseUp;
        button.MouseLeave -= OnRevealButtonMouseLeave;
        button.LostMouseCapture -= OnRevealButtonLostMouseCapture;

        passwordBox.ClearValue(RevealButtonProperty);
    }

    private static void OnRevealButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ButtonBase button || button.TemplatedParent is not PasswordBox passwordBox)
            return;

        if (!GetIsEnabled(passwordBox) || !GetHasRevealText(passwordBox))
            return;

        SetIsRevealActive(passwordBox, true);
        button.CaptureMouse();
    }

    private static void OnRevealButtonMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ButtonBase button || button.TemplatedParent is not PasswordBox passwordBox)
            return;

        if (button.IsMouseCaptured)
            button.ReleaseMouseCapture();

        SetIsRevealActive(passwordBox, false);
    }

    private static void OnRevealButtonMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not ButtonBase button || button.TemplatedParent is not PasswordBox passwordBox)
            return;

        if (button.IsMouseCaptured)
            button.ReleaseMouseCapture();

        SetIsRevealActive(passwordBox, false);
    }

    private static void OnRevealButtonLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element || element.TemplatedParent is not PasswordBox passwordBox)
            return;

        SetIsRevealActive(passwordBox, false);
    }
}
