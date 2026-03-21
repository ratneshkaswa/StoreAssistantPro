using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Shows the full text in a tooltip when a <see cref="TextBlock"/>
/// is visually trimmed with ellipsis.
/// </summary>
public static class TrimmedTextToolTip
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TrimmedTextToolTip),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty HasGeneratedToolTipProperty =
        DependencyProperty.RegisterAttached(
            "HasGeneratedToolTip",
            typeof(bool),
            typeof(TrimmedTextToolTip),
            new PropertyMetadata(false));

    private static readonly DependencyProperty IsObservingTextProperty =
        DependencyProperty.RegisterAttached(
            "IsObservingText",
            typeof(bool),
            typeof(TrimmedTextToolTip),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyDescriptor? TextPropertyDescriptor =
        DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            textBlock.Loaded += OnTextBlockChanged;
            textBlock.SizeChanged += OnTextBlockChanged;
            textBlock.Unloaded += OnTextBlockUnloaded;
            AttachTextObserver(textBlock);
            UpdateToolTip(textBlock);
        }
        else
        {
            textBlock.Loaded -= OnTextBlockChanged;
            textBlock.SizeChanged -= OnTextBlockChanged;
            textBlock.Unloaded -= OnTextBlockUnloaded;
            DetachTextObserver(textBlock);
            ClearGeneratedToolTip(textBlock);
        }
    }

    private static void OnTextBlockChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            AttachTextObserver(textBlock);
            UpdateToolTip(textBlock);
        }
    }

    private static void OnTextBlockChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            UpdateToolTip(textBlock);
        }
    }

    private static void OnObservedPropertyChanged(object? sender, EventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            UpdateToolTip(textBlock);
        }
    }

    private static void OnTextBlockUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock)
        {
            return;
        }

        DetachTextObserver(textBlock);
        ClearGeneratedToolTip(textBlock);
    }

    private static void AttachTextObserver(TextBlock textBlock)
    {
        if ((bool)textBlock.GetValue(IsObservingTextProperty))
        {
            return;
        }

        TextPropertyDescriptor?.AddValueChanged(textBlock, OnObservedPropertyChanged);
        textBlock.SetValue(IsObservingTextProperty, true);
    }

    private static void DetachTextObserver(TextBlock textBlock)
    {
        if (!(bool)textBlock.GetValue(IsObservingTextProperty))
        {
            return;
        }

        TextPropertyDescriptor?.RemoveValueChanged(textBlock, OnObservedPropertyChanged);
        textBlock.SetValue(IsObservingTextProperty, false);
    }

    private static void UpdateToolTip(TextBlock textBlock)
    {
        if (textBlock.TextTrimming == TextTrimming.None
            || string.IsNullOrWhiteSpace(textBlock.Text)
            || HasExplicitToolTip(textBlock))
        {
            ClearGeneratedToolTip(textBlock);
            return;
        }

        if (IsTrimmed(textBlock))
        {
            textBlock.SetCurrentValue(FrameworkElement.ToolTipProperty, textBlock.Text);
            textBlock.SetValue(HasGeneratedToolTipProperty, true);
        }
        else
        {
            ClearGeneratedToolTip(textBlock);
        }
    }

    private static bool HasExplicitToolTip(TextBlock textBlock)
    {
        if ((bool)textBlock.GetValue(HasGeneratedToolTipProperty))
        {
            return false;
        }

        return textBlock.ReadLocalValue(FrameworkElement.ToolTipProperty) != DependencyProperty.UnsetValue
            || SmartTooltip.GetText(textBlock) is not null
            || SmartTooltip.GetHeader(textBlock) is not null
            || SmartTooltip.GetContent(textBlock) is not null
            || SmartTooltip.GetContextKey(textBlock) is not null;
    }

    private static void ClearGeneratedToolTip(TextBlock textBlock)
    {
        if (!(bool)textBlock.GetValue(HasGeneratedToolTipProperty))
        {
            return;
        }

        textBlock.ClearValue(FrameworkElement.ToolTipProperty);
        textBlock.SetValue(HasGeneratedToolTipProperty, false);
    }

    private static bool IsTrimmed(TextBlock textBlock)
    {
        if (textBlock.ActualWidth <= 0 || textBlock.ActualHeight <= 0)
        {
            return false;
        }

        var dpi = VisualTreeHelper.GetDpi(textBlock).PixelsPerDip;
        var formattedText = CreateFormattedText(textBlock, dpi);
        var availableWidth = Math.Max(0, textBlock.ActualWidth);

        if (textBlock.TextWrapping == TextWrapping.NoWrap)
        {
            return formattedText.WidthIncludingTrailingWhitespace > availableWidth + 1;
        }

        formattedText.MaxTextWidth = availableWidth;
        var availableHeight = Math.Max(0, textBlock.ActualHeight);
        return formattedText.Height > availableHeight + 1;
    }

    private static FormattedText CreateFormattedText(TextBlock textBlock, double pixelsPerDip)
    {
        var typeface = new Typeface(
            textBlock.FontFamily,
            textBlock.FontStyle,
            textBlock.FontWeight,
            textBlock.FontStretch);

        return new FormattedText(
            textBlock.Text,
            CultureInfo.CurrentUICulture,
            textBlock.FlowDirection,
            typeface,
            textBlock.FontSize,
            Brushes.Black,
            pixelsPerDip);
    }
}
