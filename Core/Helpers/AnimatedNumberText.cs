using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

public enum AnimatedNumberFormatMode
{
    CompactNumber,
    CompactCurrency
}

/// <summary>
/// Animates numeric transitions on a <see cref="TextBlock"/> and formats
/// the intermediate value using the app's compact KPI number style.
/// </summary>
public static class AnimatedNumberText
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.RegisterAttached(
            "Value",
            typeof(double),
            typeof(AnimatedNumberText),
            new PropertyMetadata(0d, OnValueChanged));

    public static double GetValue(DependencyObject obj) =>
        (double)obj.GetValue(ValueProperty);

    public static void SetValue(DependencyObject obj, double value) =>
        obj.SetValue(ValueProperty, value);

    public static readonly DependencyProperty FormatModeProperty =
        DependencyProperty.RegisterAttached(
            "FormatMode",
            typeof(AnimatedNumberFormatMode),
            typeof(AnimatedNumberText),
            new PropertyMetadata(AnimatedNumberFormatMode.CompactNumber, OnFormattingChanged));

    public static AnimatedNumberFormatMode GetFormatMode(DependencyObject obj) =>
        (AnimatedNumberFormatMode)obj.GetValue(FormatModeProperty);

    public static void SetFormatMode(DependencyObject obj, AnimatedNumberFormatMode value) =>
        obj.SetValue(FormatModeProperty, value);

    public static readonly DependencyProperty CurrencySymbolProperty =
        DependencyProperty.RegisterAttached(
            "CurrencySymbol",
            typeof(string),
            typeof(AnimatedNumberText),
            new PropertyMetadata(string.Empty, OnFormattingChanged));

    public static string GetCurrencySymbol(DependencyObject obj) =>
        (string)obj.GetValue(CurrencySymbolProperty);

    public static void SetCurrencySymbol(DependencyObject obj, string value) =>
        obj.SetValue(CurrencySymbolProperty, value);

    private static readonly DependencyProperty AnimatedValueProperty =
        DependencyProperty.RegisterAttached(
            "AnimatedValue",
            typeof(double),
            typeof(AnimatedNumberText),
            new PropertyMetadata(0d, OnAnimatedValueChanged));

    private static readonly DependencyProperty HasAnimatedValueProperty =
        DependencyProperty.RegisterAttached(
            "HasAnimatedValue",
            typeof(bool),
            typeof(AnimatedNumberText),
            new PropertyMetadata(false));

    private static readonly DependencyProperty PendingTargetValueProperty =
        DependencyProperty.RegisterAttached(
            "PendingTargetValue",
            typeof(double),
            typeof(AnimatedNumberText),
            new PropertyMetadata(0d));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        var target = (double)e.NewValue;
        if (!textBlock.IsLoaded || IsMotionDisabled(textBlock))
        {
            textBlock.SetValue(HasAnimatedValueProperty, true);
            textBlock.SetCurrentValue(AnimatedValueProperty, target);
            return;
        }

        var from = (bool)textBlock.GetValue(HasAnimatedValueProperty)
            ? (double)textBlock.GetValue(AnimatedValueProperty)
            : (double)e.OldValue;

        AnimateValue(textBlock, from, target);
    }

    private static void OnFormattingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        UpdateText(textBlock, (double)textBlock.GetValue(AnimatedValueProperty));
    }

    private static void OnAnimatedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        textBlock.SetValue(HasAnimatedValueProperty, true);
        UpdateText(textBlock, (double)e.NewValue);
    }

    private static void AnimateValue(TextBlock textBlock, double from, double to)
    {
        textBlock.SetValue(PendingTargetValueProperty, to);

        var duration = GetDuration(textBlock, "FluentDurationSlow", TimeSpan.FromMilliseconds(250));
        if (duration == TimeSpan.Zero || Math.Abs(from - to) < 0.01d)
        {
            textBlock.BeginAnimation(AnimatedValueProperty, null);
            textBlock.SetCurrentValue(AnimatedValueProperty, to);
            return;
        }

        var animation = new DoubleAnimation(from, to, new Duration(duration))
        {
            EasingFunction = TryFindEase(textBlock, "FluentEaseDecelerate"),
            FillBehavior = FillBehavior.Stop
        };

        animation.Completed += (_, _) =>
        {
            if (!Equals(textBlock.GetValue(PendingTargetValueProperty), to))
                return;

            textBlock.BeginAnimation(AnimatedValueProperty, null);
            textBlock.SetCurrentValue(AnimatedValueProperty, to);
        };

        textBlock.BeginAnimation(AnimatedValueProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private static void UpdateText(TextBlock textBlock, double value)
    {
        var formatted = GetFormatMode(textBlock) switch
        {
            AnimatedNumberFormatMode.CompactCurrency => $"{GetCurrencySymbol(textBlock)}{FormatCompactValue(value)}",
            _ => FormatCompactValue(value)
        };

        textBlock.Text = formatted;
    }

    private static string FormatCompactValue(double value)
    {
        var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        var absolute = Math.Abs(number);
        var divisor = 1m;
        var suffix = string.Empty;

        if (absolute >= 10000000m)
        {
            divisor = 10000000m;
            suffix = "Cr";
        }
        else if (absolute >= 100000m)
        {
            divisor = 100000m;
            suffix = "L";
        }
        else if (absolute >= 1000m)
        {
            divisor = 1000m;
            suffix = "K";
        }

        var compact = number / divisor;
        if (suffix.Length > 0 && Math.Abs(compact) < 100m)
            compact = Math.Truncate(compact * 10m) / 10m;

        var format = suffix.Length == 0 || Math.Abs(compact) >= 100m ? "0" : "0.#";
        return compact.ToString(format, CultureInfo.InvariantCulture) + suffix;
    }

    private static bool IsMotionDisabled(FrameworkElement element) =>
        GetDuration(element, "FluentDurationSlow", TimeSpan.FromMilliseconds(250)) == TimeSpan.Zero;

    private static TimeSpan GetDuration(FrameworkElement element, string key, TimeSpan fallback)
    {
        if (element.TryFindResource(key) is Duration duration && duration.HasTimeSpan)
            return duration.TimeSpan;

        return fallback;
    }

    private static IEasingFunction? TryFindEase(FrameworkElement element, string key) =>
        element.TryFindResource(key) as IEasingFunction;
}
