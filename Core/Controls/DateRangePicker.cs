using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Win11-style connected date range picker with a single host surface and side-by-side calendars.
/// </summary>
public class DateRangePicker : Control
{
    static DateRangePicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DateRangePicker),
            new FrameworkPropertyMetadata(typeof(DateRangePicker)));
    }

    public static readonly DependencyProperty StartDateProperty =
        DependencyProperty.Register(
            nameof(StartDate),
            typeof(DateTime?),
            typeof(DateRangePicker),
            new FrameworkPropertyMetadata(
                DateTime.Today,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnStartDateChanged));

    public DateTime? StartDate
    {
        get => (DateTime?)GetValue(StartDateProperty);
        set => SetValue(StartDateProperty, value);
    }

    public static readonly DependencyProperty EndDateProperty =
        DependencyProperty.Register(
            nameof(EndDate),
            typeof(DateTime?),
            typeof(DateRangePicker),
            new FrameworkPropertyMetadata(
                DateTime.Today,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnEndDateChanged));

    public DateTime? EndDate
    {
        get => (DateTime?)GetValue(EndDateProperty);
        set => SetValue(EndDateProperty, value);
    }

    public static readonly DependencyProperty StartHeaderProperty =
        DependencyProperty.Register(
            nameof(StartHeader),
            typeof(string),
            typeof(DateRangePicker),
            new PropertyMetadata("From"));

    public string StartHeader
    {
        get => (string)GetValue(StartHeaderProperty);
        set => SetValue(StartHeaderProperty, value);
    }

    public static readonly DependencyProperty EndHeaderProperty =
        DependencyProperty.Register(
            nameof(EndHeader),
            typeof(string),
            typeof(DateRangePicker),
            new PropertyMetadata("To"));

    public string EndHeader
    {
        get => (string)GetValue(EndHeaderProperty);
        set => SetValue(EndHeaderProperty, value);
    }

    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(DateRangePicker),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new DateRangePickerAutomationPeer(this);

    private static void OnStartDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DateRangePicker picker ||
            picker.StartDate is not DateTime start ||
            picker.EndDate is not DateTime end ||
            end >= start)
        {
            return;
        }

        picker.EndDate = start;
    }

    private static void OnEndDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DateRangePicker picker ||
            picker.StartDate is not DateTime start ||
            picker.EndDate is not DateTime end ||
            end >= start)
        {
            return;
        }

        picker.StartDate = end;
    }

    private sealed class DateRangePickerAutomationPeer(DateRangePicker owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(DateRangePicker);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(explicitName))
                return explicitName;

            var owner = (DateRangePicker)Owner;
            return $"{owner.StartHeader} to {owner.EndHeader}";
        }
    }
}
