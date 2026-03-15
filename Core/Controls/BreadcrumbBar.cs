using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Windows 11 WinUI-style breadcrumb bar for hierarchical navigation.
/// Displays clickable path segments separated by chevrons.
/// <example>
/// <code>
/// &lt;controls:BreadcrumbBar ItemsSource="{Binding Breadcrumbs}"
///                          ItemClicked="OnBreadcrumbClicked"/&gt;
/// </code>
/// </example>
/// </summary>
public class BreadcrumbBar : ItemsControl
{
    static BreadcrumbBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BreadcrumbBar), new FrameworkPropertyMetadata(typeof(BreadcrumbBar)));
    }

    public static readonly RoutedEvent ItemClickedEvent =
        EventManager.RegisterRoutedEvent(nameof(ItemClicked), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(BreadcrumbBar));

    public event RoutedEventHandler ItemClicked
    {
        add => AddHandler(ItemClickedEvent, value);
        remove => RemoveHandler(ItemClickedEvent, value);
    }

    public static readonly DependencyProperty ItemClickCommandProperty =
        DependencyProperty.Register(nameof(ItemClickCommand), typeof(ICommand), typeof(BreadcrumbBar));

    public ICommand? ItemClickCommand
    {
        get => (ICommand?)GetValue(ItemClickCommandProperty);
        set => SetValue(ItemClickCommandProperty, value);
    }

    protected override DependencyObject GetContainerForItemOverride() => new BreadcrumbBarItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is BreadcrumbBarItem;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is BreadcrumbBarItem crumb)
        {
            crumb.IsLast = Items.IndexOf(item) == Items.Count - 1;
            crumb.ParentBar = this;
        }
    }

    internal void RaiseItemClicked(object item)
    {
        RaiseEvent(new RoutedEventArgs(ItemClickedEvent, item));
        if (ItemClickCommand is { } cmd && cmd.CanExecute(item))
            cmd.Execute(item);
    }
}

public class BreadcrumbBarItem : ContentControl
{
    private Button? _button;

    static BreadcrumbBarItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BreadcrumbBarItem), new FrameworkPropertyMetadata(typeof(BreadcrumbBarItem)));
    }

    internal BreadcrumbBar? ParentBar { get; set; }

    public static readonly DependencyProperty IsLastProperty =
        DependencyProperty.Register(nameof(IsLast), typeof(bool), typeof(BreadcrumbBarItem),
            new PropertyMetadata(false));

    public bool IsLast
    {
        get => (bool)GetValue(IsLastProperty);
        set => SetValue(IsLastProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_button is not null)
            _button.Click -= OnPartButtonClick;

        _button = GetTemplateChild("PART_Button") as Button;

        if (_button is not null)
            _button.Click += OnPartButtonClick;
    }

    private void OnPartButtonClick(object sender, RoutedEventArgs e) =>
        ParentBar?.RaiseItemClicked(Content ?? DataContext);
}
