using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Coordinates dismiss-style button execution while preventing duplicate triggers.
/// </summary>
public static class AnimatedDismissButton
{
    private static readonly DependencyProperty IsAnimatingProperty =
        DependencyProperty.RegisterAttached(
            "IsAnimating",
            typeof(bool),
            typeof(AnimatedDismissButton),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(AnimatedDismissButton),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Button button)
            return;

        if ((bool)e.NewValue)
        {
            button.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            button.PreviewKeyDown += OnPreviewKeyDown;
        }
        else
        {
            button.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            button.PreviewKeyDown -= OnPreviewKeyDown;
        }
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button button || !button.IsMouseOver)
            return;

        if (!TryBeginDismiss(button))
            return;

        e.Handled = true;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        if (sender is not Button button)
            return;

        if (!TryBeginDismiss(button))
            return;

        e.Handled = true;
    }

    private static bool TryBeginDismiss(Button button)
    {
        if ((bool)button.GetValue(IsAnimatingProperty) || button.Command is null)
            return false;

        button.SetValue(IsAnimatingProperty, true);
        RunDismissAnimation(button);
        return true;
    }

    private static void RunDismissAnimation(Button button)
    {
        ExecuteCommand(button);
        button.SetValue(IsAnimatingProperty, false);
    }

    private static void ExecuteCommand(Button button)
    {
        var command = button.Command;
        var parameter = button.CommandParameter;

        if (command?.CanExecute(parameter) == true)
            command.Execute(parameter);
    }
}
