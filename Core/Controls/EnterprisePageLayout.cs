using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Enterprise standard page layout control that enforces the canonical
/// row structure for data management pages.
/// </summary>
public class EnterprisePageLayout : ContentControl
{
    public static readonly DependencyProperty TipBannerContentProperty =
        DependencyProperty.Register(
            nameof(TipBannerContent), typeof(object), typeof(EnterprisePageLayout),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ToolbarContentProperty =
        DependencyProperty.Register(
            nameof(ToolbarContent), typeof(object), typeof(EnterprisePageLayout),
            new PropertyMetadata(null));

    public static readonly DependencyProperty BottomFormContentProperty =
        DependencyProperty.Register(
            nameof(BottomFormContent), typeof(object), typeof(EnterprisePageLayout),
            new PropertyMetadata(null));

    public static readonly DependencyProperty StatusBarContentProperty =
        DependencyProperty.Register(
            nameof(StatusBarContent), typeof(object), typeof(EnterprisePageLayout),
            new PropertyMetadata(null));

    public object? TipBannerContent
    {
        get => GetValue(TipBannerContentProperty);
        set => SetValue(TipBannerContentProperty, value);
    }

    public object? ToolbarContent
    {
        get => GetValue(ToolbarContentProperty);
        set => SetValue(ToolbarContentProperty, value);
    }

    public object? BottomFormContent
    {
        get => GetValue(BottomFormContentProperty);
        set => SetValue(BottomFormContentProperty, value);
    }

    public object? StatusBarContent
    {
        get => GetValue(StatusBarContentProperty);
        set => SetValue(StatusBarContentProperty, value);
    }

    static EnterprisePageLayout()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(EnterprisePageLayout),
            new FrameworkPropertyMetadata(typeof(EnterprisePageLayout)));

        FocusableProperty.OverrideMetadata(
            typeof(EnterprisePageLayout),
            new FrameworkPropertyMetadata(false));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new EnterprisePageLayoutAutomationPeer(this);

    private sealed class EnterprisePageLayoutAutomationPeer(EnterprisePageLayout owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(EnterprisePageLayout);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            return string.IsNullOrWhiteSpace(explicitName) ? "Page layout" : explicitName;
        }
    }
}
