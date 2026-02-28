using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that auto-dismisses a TextBlock's text after a
/// configurable delay. Designed for success/error messages that should
/// fade away without user action.
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;TextBlock Text="{Binding SuccessMessage}"
///            h:AutoDismiss.IsEnabled="True"
///            h:AutoDismiss.Seconds="4"/&gt;
/// </code>
/// </summary>
public static class AutoDismiss
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(AutoDismiss),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty SecondsProperty =
        DependencyProperty.RegisterAttached(
            "Seconds", typeof(double), typeof(AutoDismiss),
            new PropertyMetadata(4.0));

    public static double GetSeconds(DependencyObject obj) =>
        (double)obj.GetValue(SecondsProperty);

    public static void SetSeconds(DependencyObject obj, double value) =>
        obj.SetValue(SecondsProperty, value);

    // Hidden property to store the timer per element
    private static readonly DependencyProperty TimerProperty =
        DependencyProperty.RegisterAttached(
            "Timer", typeof(DispatcherTimer), typeof(AutoDismiss));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb || e.NewValue is not true)
            return;

        var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
            TextBlock.TextProperty, typeof(TextBlock));

        dpd?.AddValueChanged(tb, (_, _) => OnTextChanged(tb));
    }

    private static void OnTextChanged(TextBlock tb)
    {
        // Stop any existing timer
        if (tb.GetValue(TimerProperty) is DispatcherTimer existing)
        {
            existing.Stop();
            tb.ClearValue(TimerProperty);
        }

        var text = tb.Text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        var seconds = GetSeconds(tb);
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(seconds)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            tb.ClearValue(TimerProperty);

            // Only clear if text hasn't changed since we started
            if (tb.Text == text)
            {
                // Clear via binding source if possible
                var binding = tb.GetBindingExpression(TextBlock.TextProperty);
                if (binding?.DataItem is not null)
                {
                    var prop = binding.DataItem.GetType().GetProperty(binding.ParentBinding.Path.Path);
                    if (prop is not null && prop.CanWrite && prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(binding.DataItem, string.Empty);
                        return;
                    }
                }
                tb.Text = string.Empty;
            }
        };
        tb.SetValue(TimerProperty, timer);
        timer.Start();
    }
}
