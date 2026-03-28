namespace StoreAssistantPro.Tests.Helpers;

public sealed class SpeedFirstMotionStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Global_Styles_Should_Avoid_Remaining_Command_Row_And_Fab_Animations()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.DoesNotContain("CommandSpinnerStoryboard", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FabScale", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"SelectionIndicator\"", source, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"SelectionIndicator\" Property=\"Opacity\" Value=\"1\"/>", source, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Opacity\" Value=\"1\"/>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Toggle_And_Focus_Templates_Should_Use_Static_State_Changes()
    {
        var toggleStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "ToggleSwitch.xaml"));
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Setter TargetName=\"Thumb\" Property=\"HorizontalAlignment\" Value=\"Right\"/>", toggleStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"ThumbTranslate\"", toggleStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"FocusIndicator\"", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"ChevronRotate\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"Chevron\" Property=\"Text\" Value=\"&#xE70E;\"/>", fluentTheme, StringComparison.Ordinal);
    }

    [Fact]
    public void Active_Filter_Chips_Should_Not_Animate_Scale()
    {
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var chipStyle = GetStyleBlock(posStyles, "<Style x:Key=\"ActiveFilterChipButtonStyle\" TargetType=\"Button\">");

        Assert.DoesNotContain("ChipScale", chipStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", chipStyle, StringComparison.Ordinal);
    }

    [Fact]
    public void Pos_Buttons_And_Top_Shell_Quick_Actions_Should_Use_Static_State_Changes()
    {
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        var quickActionStyle = GetStyleBlock(posStyles, "<Style x:Key=\"QuickActionButtonStyle\" TargetType=\"Button\">");
        var enterpriseActionStyle = GetStyleBlock(posStyles, "<Style x:Key=\"EnterpriseActionButtonStyle\" TargetType=\"Button\">");
        var segmentedFilterStyle = GetStyleBlock(posStyles, "<Style x:Key=\"SegmentedFilterButtonStyle\" TargetType=\"Button\">");
        var toolbarLinkStyle = GetStyleBlock(posStyles, "<Style x:Key=\"ToolbarLinkButtonStyle\" TargetType=\"Button\">");

        Assert.DoesNotContain("RootScale", quickActionStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", quickActionStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginStoryboard", quickActionStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("BdScale", enterpriseActionStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", enterpriseActionStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginStoryboard", enterpriseActionStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("BdScale", segmentedFilterStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", segmentedFilterStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginStoryboard", segmentedFilterStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("BdScale", toolbarLinkStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", toolbarLinkStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginStoryboard", toolbarLinkStyle, StringComparison.Ordinal);

        Assert.Contains("<Style x:Key=\"QuickActionOverflowButtonStyle\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigationRailToggleButtonStyle", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginStoryboard", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("RotateTransform", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Helper_Behaviors_Should_Avoid_Runtime_Animation_Paths()
    {
        var activeAreaHighlight = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "ActiveAreaHighlight.cs"));
        var adaptiveWorkspace = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "AdaptiveWorkspace.cs"));
        var calmTransition = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "CalmTransition.cs"));
        var billingDimBehavior = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "BillingDimBehavior.cs"));
        var animatedNumberText = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "AnimatedNumberText.cs"));
        var inlineTipBanner = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "InlineTipBanner.cs"));
        var autoGrowTextBox = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "AutoGrowTextBox.cs"));
        var fluentExpander = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "FluentExpander.cs"));
        var expandableText = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ExpandableTextBlock.cs"));
        var cardHover = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "CardHover.cs"));
        var notificationBadge = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "NotificationBadgeBehavior.cs"));
        var popupFlyoutMotion = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "PopupFlyoutMotion.cs"));
        var toastSwipeDismiss = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "ToastSwipeDismiss.cs"));
        var validationFeedback = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "ValidationFeedback.cs"));
        var motion = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "Motion.cs"));
        var progressRing = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ProgressRing.cs"));
        var smoothScroll = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "SmoothScroll.cs"));
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.DoesNotContain("DoubleAnimation", activeAreaHighlight, StringComparison.Ordinal);
        Assert.DoesNotContain("ColorAnimation", activeAreaHighlight, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", adaptiveWorkspace, StringComparison.Ordinal);
        Assert.DoesNotContain("ColorAnimation", adaptiveWorkspace, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", calmTransition, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveDuration(", calmTransition, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveEase(", calmTransition, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", billingDimBehavior, StringComparison.Ordinal);
        Assert.DoesNotContain("AnimateOpacity(", billingDimBehavior, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", animatedNumberText, StringComparison.Ordinal);
        Assert.DoesNotContain("PendingTargetValueProperty", animatedNumberText, StringComparison.Ordinal);
        Assert.Contains("DismissImmediately()", inlineTipBanner, StringComparison.Ordinal);
        Assert.DoesNotContain("AnimateDismiss()", inlineTipBanner, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", autoGrowTextBox, StringComparison.Ordinal);
        Assert.DoesNotContain("AnimateExpand()", fluentExpander, StringComparison.Ordinal);
        Assert.DoesNotContain("AnimateCollapse()", fluentExpander, StringComparison.Ordinal);
        Assert.DoesNotContain("AnimateMeasuredState(", expandableText, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", cardHover, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginAnimation", cardHover, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", notificationBadge, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayBellPulse", notificationBadge, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayBadgeEntrance", notificationBadge, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayBadgeBounce", notificationBadge, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", popupFlyoutMotion, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveOriginOffset", popupFlyoutMotion, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", toastSwipeDismiss, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginAnimation", toastSwipeDismiss, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimationUsingKeyFrames", validationFeedback, StringComparison.Ordinal);
        Assert.Contains("private static void OnNoOpChanged", motion, StringComparison.Ordinal);
        Assert.Contains("speed-first mode disables all decorative motion behaviors", motion, StringComparison.Ordinal);
        Assert.DoesNotContain("Storyboard", progressRing, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", progressRing, StringComparison.Ordinal);
        Assert.Contains("ApplyActiveState();", progressRing, StringComparison.Ordinal);
        Assert.DoesNotContain("ScrollViewerOffsetMediator", smoothScroll, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", smoothScroll, StringComparison.Ordinal);
        Assert.DoesNotContain("h:ClickRipple.IsEnabled", fluentTheme, StringComparison.Ordinal);
    }

    private static string GetStyleBlock(string source, string marker)
    {
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return source;

        var end = source.IndexOf("</Style>", start, StringComparison.Ordinal);
        if (end < 0)
            return source[start..];

        return source[start..(end + "</Style>".Length)];
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

        throw new InvalidOperationException(
            "Could not find solution root from " + AppContext.BaseDirectory);
    }
}
