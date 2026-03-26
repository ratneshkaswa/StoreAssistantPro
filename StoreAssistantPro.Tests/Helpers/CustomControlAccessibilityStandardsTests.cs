namespace StoreAssistantPro.Tests.Helpers;

public sealed class CustomControlAccessibilityStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Custom_Controls_Should_Assign_Automation_Names()
    {
        var appBranding = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "AppBranding.cs"));
        var breadcrumb = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "BreadcrumbBar.cs"));
        var splitButton = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "SplitButton.cs"));
        var numberBox = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "NumberBox.cs"));
        var dateRangePicker = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "DateRangePicker.cs"));
        var emptyState = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "EmptyStateOverlay.cs"));
        var errorState = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "ErrorStateOverlay.cs"));
        var enterpriseLayout = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "EnterprisePageLayout.cs"));
        var infoBar = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "InfoBar.cs"));
        var loadingOverlay = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "LoadingOverlay.cs"));
        var fluentExpander = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "FluentExpander.cs"));
        var inlineTipBanner = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "InlineTipBanner.cs"));
        var toastHost = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "ToastHost.cs"));
        var progressRing = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "ProgressRing.cs"));
        var responsiveContent = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "ResponsiveContentControl.cs"));
        var setupFormCard = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "SetupFormCard.cs"));
        var expandableText = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "ExpandableTextBlock.cs"));
        var viewportPanel = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "ViewportConstrainedPanel.cs"));

        Assert.Contains("OnCreateAutomationPeer() => new AppBrandingAutomationPeer(this)", appBranding, StringComparison.Ordinal);
        Assert.Contains("Store Assistant Pro", appBranding, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.SetName(_button, GetAutomationName())", breadcrumb, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new BreadcrumbBarAutomationPeer(this)", breadcrumb, StringComparison.Ordinal);
        Assert.Contains("Navigate to", breadcrumb, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.SetName(_primaryButton, GetPrimaryAutomationName())", splitButton, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new SplitButtonAutomationPeer(this)", splitButton, StringComparison.Ordinal);
        Assert.Contains("Primary action", splitButton, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new NumberBoxAutomationPeer(this)", numberBox, StringComparison.Ordinal);
        Assert.Contains("Increase value", numberBox, StringComparison.Ordinal);
        Assert.Contains("Decrease value", numberBox, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new DateRangePickerAutomationPeer(this)", dateRangePicker, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new EmptyStateOverlayAutomationPeer(this)", emptyState, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new ErrorStateOverlayAutomationPeer(this)", errorState, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new EnterprisePageLayoutAutomationPeer(this)", enterpriseLayout, StringComparison.Ordinal);
        Assert.Contains("Page layout", enterpriseLayout, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new InfoBarAutomationPeer(this)", infoBar, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new LoadingOverlayAutomationPeer(this)", loadingOverlay, StringComparison.Ordinal);
        Assert.Contains("Loading content", loadingOverlay, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new FluentExpanderAutomationPeer(this)", fluentExpander, StringComparison.Ordinal);
        Assert.Contains("expanded", fluentExpander, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new InlineTipBannerAutomationPeer(this)", inlineTipBanner, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new ToastHostAutomationPeer(this)", toastHost, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new ProgressRingAutomationPeer(this)", progressRing, StringComparison.Ordinal);
        Assert.Contains("Loading progress", progressRing, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new ResponsiveContentControlAutomationPeer(this)", responsiveContent, StringComparison.Ordinal);
        Assert.Contains("Page content", responsiveContent, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new SetupFormCardAutomationPeer(this)", setupFormCard, StringComparison.Ordinal);
        Assert.Contains("Setup form", setupFormCard, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new ExpandableTextBlockAutomationPeer(this)", expandableText, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new ViewportConstrainedPanelAutomationPeer(this)", viewportPanel, StringComparison.Ordinal);
        Assert.Contains("Viewport content host", viewportPanel, StringComparison.Ordinal);
    }

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0 ||
                Directory.GetFiles(dir, "*.slnx").Length > 0)
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find solution root from " + AppContext.BaseDirectory);
    }
}
