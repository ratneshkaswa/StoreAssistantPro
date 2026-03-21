using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Copies plain tooltip text into <see cref="AutomationProperties.NameProperty"/>
/// when a control does not already have an explicit accessibility name.
/// This keeps icon-only buttons screen-reader friendly without forcing
/// page-by-page AutomationProperties markup everywhere.
/// </summary>
public static class AutomationNameFallback
{
    public static readonly DependencyProperty UseToolTipFallbackProperty =
        DependencyProperty.RegisterAttached(
            "UseToolTipFallback",
            typeof(bool),
            typeof(AutomationNameFallback),
            new PropertyMetadata(false, OnUseToolTipFallbackChanged));

    private static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached(
            "IsAttached",
            typeof(bool),
            typeof(AutomationNameFallback),
            new PropertyMetadata(false));

    private static readonly DependencyProperty IsObservingToolTipProperty =
        DependencyProperty.RegisterAttached(
            "IsObservingToolTip",
            typeof(bool),
            typeof(AutomationNameFallback),
            new PropertyMetadata(false));

    private static readonly DependencyProperty AppliedNameProperty =
        DependencyProperty.RegisterAttached(
            "AppliedName",
            typeof(string),
            typeof(AutomationNameFallback),
            new PropertyMetadata(null));

    private static readonly DependencyPropertyDescriptor? ToolTipPropertyDescriptor =
        DependencyPropertyDescriptor.FromProperty(FrameworkElement.ToolTipProperty, typeof(FrameworkElement));

    public static bool GetUseToolTipFallback(DependencyObject obj) =>
        (bool)obj.GetValue(UseToolTipFallbackProperty);

    public static void SetUseToolTipFallback(DependencyObject obj, bool value) =>
        obj.SetValue(UseToolTipFallbackProperty, value);

    private static void OnUseToolTipFallbackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
        {
            Attach(element);
            UpdateAutomationName(element);
            return;
        }

        Detach(element);
        ClearGeneratedName(element);
    }

    private static void Attach(FrameworkElement element)
    {
        if ((bool)element.GetValue(IsAttachedProperty))
            return;

        element.Loaded += OnElementLoaded;
        element.Unloaded += OnElementUnloaded;
        AttachToolTipObserver(element);
        element.SetValue(IsAttachedProperty, true);
    }

    private static void Detach(FrameworkElement element)
    {
        if (!(bool)element.GetValue(IsAttachedProperty))
            return;

        element.Loaded -= OnElementLoaded;
        element.Unloaded -= OnElementUnloaded;
        DetachToolTipObserver(element);
        element.SetValue(IsAttachedProperty, false);
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        AttachToolTipObserver(element);
        UpdateAutomationName(element);
    }

    private static void OnElementUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
            DetachToolTipObserver(element);
    }

    private static void AttachToolTipObserver(FrameworkElement element)
    {
        if ((bool)element.GetValue(IsObservingToolTipProperty))
            return;

        ToolTipPropertyDescriptor?.AddValueChanged(element, OnObservedToolTipChanged);
        element.SetValue(IsObservingToolTipProperty, true);
    }

    private static void DetachToolTipObserver(FrameworkElement element)
    {
        if (!(bool)element.GetValue(IsObservingToolTipProperty))
            return;

        ToolTipPropertyDescriptor?.RemoveValueChanged(element, OnObservedToolTipChanged);
        element.SetValue(IsObservingToolTipProperty, false);
    }

    private static void OnObservedToolTipChanged(object? sender, EventArgs e)
    {
        if (sender is FrameworkElement element)
            UpdateAutomationName(element);
    }

    private static void UpdateAutomationName(FrameworkElement element)
    {
        var currentName = Normalize(AutomationProperties.GetName(element));
        var appliedName = Normalize(element.GetValue(AppliedNameProperty) as string);

        if (currentName is not null && currentName != appliedName)
        {
            element.ClearValue(AppliedNameProperty);
            return;
        }

        var tooltipName = ResolveToolTipText(element.ToolTip);
        if (tooltipName is null)
        {
            ClearGeneratedName(element);
            return;
        }

        AutomationProperties.SetName(element, tooltipName);
        element.SetValue(AppliedNameProperty, tooltipName);
    }

    private static void ClearGeneratedName(FrameworkElement element)
    {
        var appliedName = Normalize(element.GetValue(AppliedNameProperty) as string);
        var currentName = Normalize(AutomationProperties.GetName(element));

        if (appliedName is not null && currentName == appliedName)
            element.ClearValue(AutomationProperties.NameProperty);

        element.ClearValue(AppliedNameProperty);
    }

    private static string? ResolveToolTipText(object? value) =>
        value switch
        {
            null => null,
            string text => Normalize(text),
            ToolTip toolTip => ResolveToolTipText(toolTip.Content),
            TextBlock textBlock => Normalize(textBlock.Text),
            AccessText accessText => Normalize(accessText.Text),
            Run run => Normalize(run.Text),
            ContentControl contentControl => ResolveToolTipText(contentControl.Content),
            _ => Normalize(value.ToString())
        };

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}
