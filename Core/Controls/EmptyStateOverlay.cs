using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Reusable empty-state overlay that displays an icon, title, short
/// description, and an optional action button when a data collection
/// contains zero items.
/// </summary>
public class EmptyStateOverlay : Control
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon), typeof(string), typeof(EmptyStateOverlay),
            new PropertyMetadata("📋"));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(EmptyStateOverlay),
            new PropertyMetadata("No items found"));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(EmptyStateOverlay),
            new PropertyMetadata(string.Empty));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(
            nameof(ActionText), typeof(string), typeof(EmptyStateOverlay),
            new PropertyMetadata(null));

    public string? ActionText
    {
        get => (string?)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(
            nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateOverlay));

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public static readonly DependencyProperty ItemCountProperty =
        DependencyProperty.Register(
            nameof(ItemCount), typeof(int), typeof(EmptyStateOverlay),
            new PropertyMetadata(0, OnItemCountChanged));

    public int ItemCount
    {
        get => (int)GetValue(ItemCountProperty);
        set => SetValue(ItemCountProperty, value);
    }

    static EmptyStateOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(EmptyStateOverlay),
            new FrameworkPropertyMetadata(typeof(EmptyStateOverlay)));

        FocusableProperty.OverrideMetadata(
            typeof(EmptyStateOverlay),
            new FrameworkPropertyMetadata(false));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new EmptyStateOverlayAutomationPeer(this);

    private static void OnItemCountChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EmptyStateOverlay overlay)
            overlay.UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        Visibility = ItemCount == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisibility();
    }

    private sealed class EmptyStateOverlayAutomationPeer(EmptyStateOverlay owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(EmptyStateOverlay);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(explicitName))
                return explicitName;

            var owner = (EmptyStateOverlay)Owner;
            return string.IsNullOrWhiteSpace(owner.Description)
                ? owner.Title
                : $"{owner.Title}: {owner.Description}";
        }
    }
}
