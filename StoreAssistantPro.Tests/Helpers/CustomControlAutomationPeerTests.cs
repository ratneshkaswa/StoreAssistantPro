using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using StoreAssistantPro.Core.Controls;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class CustomControlAutomationPeerTests
{
    [Fact]
    public void Custom_Controls_Should_Expose_Named_Automation_Peers()
    {
        WpfTestApplication.Run(() =>
        {
            AssertPeer(new AppBranding { Subtitle = "Counter workspace" }, nameof(AppBranding), "Store Assistant Pro, Counter workspace");
            AssertPeer(new BreadcrumbBar(), nameof(BreadcrumbBar), "Breadcrumb navigation");
            AssertPeer(new SplitButton { Content = "Create PO" }, nameof(SplitButton), "Create PO");
            AssertPeer(new NumberBox { PlaceholderText = "Quantity" }, nameof(NumberBox), "Quantity");
            AssertPeer(new DateRangePicker(), nameof(DateRangePicker), "From to To");
            AssertPeer(new EmptyStateOverlay { Title = "No products yet", Description = "Add a product to get started." }, nameof(EmptyStateOverlay), "No products yet: Add a product to get started.");
            AssertPeer(new ErrorStateOverlay { Title = "Load failed", Description = "Try again in a moment." }, nameof(ErrorStateOverlay), "Load failed: Try again in a moment.");
            AssertPeer(new EnterprisePageLayout(), nameof(EnterprisePageLayout), "Page layout");
            AssertPeer(new InfoBar { Title = "Saved", Message = "Settings updated." }, nameof(InfoBar), "Saved: Settings updated.");
            AssertPeer(new LoadingOverlay { Message = "Loading sale history..." }, nameof(LoadingOverlay), "Loading sale history...");
            AssertPeer(new FluentExpander { Header = "Advanced options", IsExpanded = true }, nameof(FluentExpander), "Advanced options, expanded");
            AssertPeer(new InlineTipBanner { Title = "Quick tip", TipText = "Use Ctrl+K to search." }, nameof(InlineTipBanner), "Quick tip: Use Ctrl+K to search.");
            AssertPeer(new ToastHost(), nameof(ToastHost), "Notifications");
            AssertPeer(new ProgressRing { IsActive = true }, nameof(ProgressRing), "Loading progress");
            AssertPeer(new ResponsiveContentControl(), nameof(ResponsiveContentControl), "Page content");
            AssertPeer(new SetupFormCard(), nameof(SetupFormCard), "Setup form");
            AssertPeer(new ExpandableTextBlock { Text = "Long expandable content for automation." }, nameof(ExpandableTextBlock), "Long expandable content for automation.");
            AssertPeer(new ViewportConstrainedPanel(), nameof(ViewportConstrainedPanel), "Viewport content host");
        });
    }

    private static void AssertPeer(FrameworkElement element, string expectedClassName, string expectedName)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(element)
            ?? throw new InvalidOperationException($"No automation peer created for {expectedClassName}.");

        Assert.Equal(expectedClassName, peer.GetClassName());
        Assert.Equal(expectedName, peer.GetName());
    }
}
