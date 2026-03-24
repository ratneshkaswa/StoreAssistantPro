using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class WindowShellStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SecondaryWindows_Should_Inherit_BaseDialogWindow()
    {
        var exceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MainWindow.xaml"
        };

        var violations = Directory
            .EnumerateFiles(Path.Combine(SolutionRoot, "Modules"), "*Window.xaml", SearchOption.AllDirectories)
            .Where(path => !exceptions.Contains(Path.GetFileName(path)))
            .Where(path => !File.ReadAllText(path).Contains("<core:BaseDialogWindow", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(SolutionRoot, path))
            .OrderBy(path => path)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Secondary windows must inherit BaseDialogWindow so the shared two-tone shell is applied.\n"
            + string.Join("\n", violations));
    }

    [Theory]
    [InlineData("Modules\\Authentication\\Views\\LoginView.xaml")]
    public void StartupViews_Should_Use_AppBackground_And_SurfaceCard(string relativePath)
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains("AppBackgroundBrush", content, StringComparison.Ordinal);
        Assert.Contains("FluentSurface", content, StringComparison.Ordinal);
        Assert.Contains("FluentSurfaceStroke", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_Should_Use_AppShell_SurfacePattern()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("Background=\"{StaticResource AppBackgroundBrush}\"", content, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource FluentSurface}\"", content, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"{StaticResource FluentSurfaceStroke}\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellBackground_Should_Use_NoiseTexture_And_Subtle_WindowFrame()
    {
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("<DrawingBrush x:Key=\"AppBackgroundBrush\"", designSystem, StringComparison.Ordinal);
        Assert.Contains("WindowEdgeFrameBrush", designSystem, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"{StaticResource WindowEdgeFrameBrush}\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_Should_Host_ToastOverlay()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var viewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<controls:ToastHost", content, StringComparison.Ordinal);
        Assert.Contains("Toasts=\"{Binding ToastService.Toasts}\"", content, StringComparison.Ordinal);
        Assert.Contains("public IToastService ToastService { get; }", viewModel, StringComparison.Ordinal);
        Assert.Contains("ToastService = toastService;", viewModel, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"{x:Type controls:ToastHost}\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{TemplateBinding Toasts}\"", fluentTheme, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowNotificationsPopup_Should_Avoid_Hardcoded_Offsets()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var codeBehind = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));

        Assert.DoesNotContain("NotificationPopupOffset", content, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"NotificationsPopup\"", content, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AnchoredFlyoutPopupStyle}\"", content, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FlyoutPopupSurfaceStyle}\"", content, StringComparison.Ordinal);
        Assert.Contains("Opened=\"OnNotificationsPopupOpened\"", content, StringComparison.Ordinal);
        Assert.Contains("UpdateNotificationsPopupLayout();", codeBehind, StringComparison.Ordinal);
        Assert.Contains("NotificationsPopup.HorizontalOffset", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowNotificationsPopup_Should_Allow_LongMessages_To_Stay_Reachable()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("ExpandableTextBlock", content, StringComparison.Ordinal);
    }
    [Fact]
    public void TopLevelWindows_Should_Use_Shared_WindowSizingService()
    {
        var mainWindowCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));

        Assert.Contains("sizingService.ConfigureMainWindow(this);", mainWindowCode, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_Should_Clear_RequestClose_On_DataContext_Swap_And_Close()
    {
        var mainWindowCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));
        var mainViewModelCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));

        Assert.Contains("DataContextChanged += OnDataContextChanged;", mainWindowCode, StringComparison.Ordinal);
        Assert.Contains("Closed += OnClosed;", mainWindowCode, StringComparison.Ordinal);
        Assert.Contains("if (e.OldValue is MainViewModel oldVm)", mainWindowCode, StringComparison.Ordinal);
        Assert.Contains("oldVm.RequestClose = null;", mainWindowCode, StringComparison.Ordinal);
        Assert.Contains("_boundViewModel.RequestClose = null;", mainWindowCode, StringComparison.Ordinal);
        Assert.Contains("InputBindings.Clear();", mainWindowCode, StringComparison.Ordinal);
        Assert.Contains("RequestClose = null;", mainViewModelCode, StringComparison.Ordinal);
    }

    [Fact]
    public void AppMessageDialog_Should_Use_Standard_DialogLayout()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "AppMessageDialog.xaml"));
        var codeBehind = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "AppMessageDialog.xaml.cs"));

        Assert.Contains("Style=\"{StaticResource DialogHeaderContentSurfaceStyle}\"", content, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DialogTitleText\"", content, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MessageText\"", content, StringComparison.Ordinal);
        Assert.Contains("<RowDefinition Height=\"Auto\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Viewbox Width=\"16\"", content, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"{StaticResource FluentSurfaceStroke}\"", content, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SecondaryButtonStyle}\"", content, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PrimaryButtonStyle}\"", content, StringComparison.Ordinal);
        Assert.Contains("SizeToContent = SizeToContent.Height;", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void AppMessageDialog_Should_Wrap_Long_Unbroken_Messages()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "AppMessageDialog.xaml"));

        Assert.Contains("TextWrapping=\"WrapWithOverflow\"", content, StringComparison.Ordinal);
    }
    [Fact]
    public void FormRowLabelStyle_Should_Be_LeftAligned()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var start = content.IndexOf("<Style x:Key=\"FormRowLabelStyle\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "FormRowLabelStyle was not found.");
        var end = content.IndexOf("</Style>", start, StringComparison.Ordinal);
        Assert.True(end > start, "FormRowLabelStyle closing tag was not found.");

        var styleBlock = content[start..(end + "</Style>".Length)];

        Assert.Contains("<Setter Property=\"HorizontalAlignment\" Value=\"Left\"/>", styleBlock, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"TextAlignment\" Value=\"Left\"/>", styleBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("<Setter Property=\"HorizontalAlignment\" Value=\"Right\"/>", styleBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowViews_Should_Not_Use_KpiSummaryCards()
    {
        var windowFiles = Directory.EnumerateFiles(
            Path.Combine(SolutionRoot, "Modules"),
            "*Window.xaml",
            SearchOption.AllDirectories);

        var violations = windowFiles
            .Select(path => new
            {
                Path = path,
                Content = File.ReadAllText(path)
            })
            .Where(file =>
                file.Content.Contains("StatCardStyle", StringComparison.Ordinal) ||
                file.Content.Contains("summary card", StringComparison.OrdinalIgnoreCase) ||
                file.Content.Contains("FluentKpiBlueMuted", StringComparison.Ordinal) ||
                file.Content.Contains("FluentKpiGreenMuted", StringComparison.Ordinal) ||
                file.Content.Contains("FluentKpiOrangeMuted", StringComparison.Ordinal) ||
                file.Content.Contains("FluentKpiTealMuted", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(SolutionRoot, file.Path))
            .OrderBy(path => path)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Window views should not use colored KPI summary cards.\n" + string.Join("\n", violations));
    }

    [Fact]
    public void WorkspaceView_Should_Be_Minimal_Dashboard()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml"));

        Assert.Contains("DashboardSummary", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Session Overview", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Start Here", content, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenFirmManagementCommand", content, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenUserManagementCommand", content, StringComparison.Ordinal);
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
