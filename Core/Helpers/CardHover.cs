using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that adds Windows 11-style interactive hover to any
/// <see cref="Border"/> (cards, tiles). On mouse enter the card subtly
/// lifts with a shadow increase and slight scale. On press it shrinks
/// back slightly for a "click" feel.
/// <example>
/// <code>&lt;Border Style="{StaticResource FluentCardStyle}" h:CardHover.IsEnabled="True"/&gt;</code>
/// </example>
/// </summary>
public static class CardHover
{
    private static readonly Duration HoverDuration = new(TimeSpan.FromMilliseconds(90));
    private static readonly IEasingFunction Easing = new CubicEase { EasingMode = EasingMode.EaseOut };

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(CardHover),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border) return;

        if ((bool)e.NewValue)
        {
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            border.RenderTransform = new ScaleTransform(1, 1);
            border.Cursor = Cursors.Hand;
            border.MouseEnter += OnMouseEnter;
            border.MouseLeave += OnMouseLeave;
            border.PreviewMouseLeftButtonDown += OnMouseDown;
            border.PreviewMouseLeftButtonUp += OnMouseUp;
        }
        else
        {
            if (Equals(border.Cursor, Cursors.Hand))
                border.ClearValue(FrameworkElement.CursorProperty);

            border.MouseEnter -= OnMouseEnter;
            border.MouseLeave -= OnMouseLeave;
            border.PreviewMouseLeftButtonDown -= OnMouseDown;
            border.PreviewMouseLeftButtonUp -= OnMouseUp;
        }
    }

    private static void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Border border) return;
        AnimateScale(border, 1.006);
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not Border border) return;
        AnimateScale(border, 1.0);
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        AnimateScale(border, 0.994);
    }

    private static void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        AnimateScale(border, 1.006);
    }

    private static void AnimateScale(Border border, double scale)
    {
        if (border.RenderTransform is not ScaleTransform transform)
        {
            transform = new ScaleTransform(1, 1);
            border.RenderTransform = transform;
        }

        var animX = new DoubleAnimation(scale, HoverDuration) { EasingFunction = Easing };
        var animY = new DoubleAnimation(scale, HoverDuration) { EasingFunction = Easing };

        transform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }
}
