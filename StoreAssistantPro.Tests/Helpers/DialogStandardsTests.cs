using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class DialogStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void ApplicationCode_Should_NotUse_Raw_MessageBox_Show()
    {
        var violations = Directory
            .EnumerateFiles(SolutionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("StoreAssistantPro.Tests", StringComparison.Ordinal))
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("MessageBox.Show", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(SolutionRoot, path))
            .OrderBy(path => path)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Use AppMessageDialog/AppDialogPresenter instead of raw MessageBox.Show.\n" + string.Join("\n", violations));
    }

    [Fact]
    public void BaseDialogWindow_Should_Use_Shared_OverflowScrollHost()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Base", "BaseDialogWindow.cs"));

        Assert.Contains("ViewportConstrainedPanel", content, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility = ScrollBarVisibility.Auto", content, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility = ScrollBarVisibility.Auto", content, StringComparison.Ordinal);
        Assert.Contains("UseLayoutRounding = true", content, StringComparison.Ordinal);
        Assert.Contains("SnapsToDevicePixels = true", content, StringComparison.Ordinal);
        Assert.Contains("EnableOverflowScrollHost", content, StringComparison.Ordinal);
        Assert.Contains("Loaded += (_, _) => EnsureOverflowScrollHost();", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BaseDialogWindow_Should_Apply_Shared_Owner_Smoke_Overlay()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Base", "BaseDialogWindow.cs"));
        var helper = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "DialogOwnerSmokeOverlay.cs"));
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("EnableOwnerSmokeOverlay", content, StringComparison.Ordinal);
        Assert.Contains("Loaded += (_, _) => ApplyOwnerSmokeOverlay();", content, StringComparison.Ordinal);
        Assert.Contains("Closed += (_, _) => ClearOwnerSmokeOverlay();", content, StringComparison.Ordinal);
        Assert.Contains("DialogOwnerSmokeOverlay.Show(Owner, overlayBrush)", content, StringComparison.Ordinal);
        Assert.Contains("AdornerLayer.GetAdornerLayer", helper, StringComparison.Ordinal);
        Assert.Contains("DialogOwnerSmokeOverlayBrush", designSystem, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Dialogs_Should_Use_Frosted_Header_Surface()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var messageDialog = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "AppMessageDialog.xaml"));
        var masterPinDialog = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "MasterPinDialog.xaml"));
        var printPreview = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml"));

        Assert.Contains("DialogHeaderBackdropStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("BlurEffect Radius=\"8\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DialogHeaderContentSurfaceStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DialogFooterBarStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DialogFooterActionRowStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DialogFooterPrimaryButtonStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DialogFooterSecondaryButtonStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DialogFooterCloseButtonStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DialogHeaderBackdropStyle", messageDialog, StringComparison.Ordinal);
        Assert.Contains("DialogHeaderBackdropStyle", masterPinDialog, StringComparison.Ordinal);
        Assert.Contains("DialogHeaderBackdropStyle", printPreview, StringComparison.Ordinal);
        Assert.Contains("DialogFooterBarStyle", messageDialog, StringComparison.Ordinal);
        Assert.Contains("DialogFooterBarStyle", masterPinDialog, StringComparison.Ordinal);
        Assert.Contains("DialogFooterBarStyle", printPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Dialogs_Should_Hide_Caption_Text_And_Use_InContent_Headers()
    {
        var messageDialogXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "AppMessageDialog.xaml"));
        var messageDialogCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "AppMessageDialog.xaml.cs"));
        var masterPinDialogXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "MasterPinDialog.xaml"));
        var printPreviewXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml"));
        var printPreviewCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml.cs"));

        Assert.Contains("Title=\"\"", messageDialogXaml, StringComparison.Ordinal);
        Assert.Contains("DialogTitleText.Text = dialogTitle;", messageDialogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("DialogTitleText.Text = Title;", messageDialogCode, StringComparison.Ordinal);
        Assert.Contains("Title=\"\"", masterPinDialogXaml, StringComparison.Ordinal);
        Assert.Contains("Title=\"\"", printPreviewXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Title = $\"Print Preview - {title}\";", printPreviewCode, StringComparison.Ordinal);
        Assert.Contains("TitleText.Text = title;", printPreviewCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Dialogs_Should_Use_Semantic_Footer_Action_Styles()
    {
        var messageDialog = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "AppMessageDialog.xaml"));
        var masterPinDialog = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "MasterPinDialog.xaml"));
        var printPreview = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml"));

        Assert.Contains("Style=\"{StaticResource DialogFooterBarStyle}\"", messageDialog, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterActionRowStyle}\"", messageDialog, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterSecondaryButtonStyle}\"", messageDialog, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterPrimaryButtonStyle}\"", messageDialog, StringComparison.Ordinal);
        Assert.Contains("IsCancel=\"True\"", messageDialog, StringComparison.Ordinal);
        Assert.Contains("IsDefault=\"True\"", messageDialog, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource DialogFooterBarStyle}\"", masterPinDialog, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterActionRowStyle}\"", masterPinDialog, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterSecondaryButtonStyle}\"", masterPinDialog, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterPrimaryButtonStyle}\"", masterPinDialog, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource DialogFooterBarStyle}\"", printPreview, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterActionRowStyle}\"", printPreview, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterCloseButtonStyle}\"", printPreview, StringComparison.Ordinal);
        Assert.Contains("IsDefault=\"True\"", printPreview, StringComparison.Ordinal);
        Assert.Contains("IsCancel=\"True\"", printPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginView_Should_Use_OverflowScrollHost_For_MessageGrowth()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));

        Assert.Contains("<ScrollViewer VerticalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", content, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{Binding ViewportWidth, RelativeSource={RelativeSource AncestorType=ScrollViewer}}\"", content, StringComparison.Ordinal);
        Assert.Contains("MinHeight=\"{Binding ViewportHeight, RelativeSource={RelativeSource AncestorType=ScrollViewer}}\"", content, StringComparison.Ordinal);
        Assert.Contains("PanningMode=\"VerticalOnly\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("<controls:ViewportConstrainedPanel", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SaleHistoryView_Should_Allow_Horizontal_ReceiptPreview_Scrolling()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"));

        Assert.Contains("Style=\"{StaticResource ReadOnlyPreviewScrollViewerStyle}\"", content, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyPreviewTextStyle}\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_Should_Use_ResponsiveWorkspaceHost_For_OverflowSafePages()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("<controls:ResponsiveContentControl Content=\"{Binding CurrentView}\"/>", content, StringComparison.Ordinal);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DenseDialogForms_Should_Use_Vertical_Only_Responsive_Scroll_Hosts()
    {
        var files = new[]
        {
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml")
        };

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", content, StringComparison.Ordinal);
            Assert.Contains("PanningMode=\"VerticalOnly\"", content, StringComparison.Ordinal);
            Assert.DoesNotContain("PanningMode=\"Both\"", content, StringComparison.Ordinal);
        }
    }
    [Fact]
    public void TopLevelWindowIcons_Should_Be_Applied_In_CodeBehind()
    {
        var mainWindowXaml = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var mainWindowCode = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));

        Assert.DoesNotContain("Icon=\"pack://application:,,,/Assets/app.ico\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("WindowIconHelper.Apply(this);", mainWindowCode, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginView_Should_Unsubscribe_ViewModel_Handlers_On_Unload()
    {
        var loginViewCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml.cs"));

        Assert.Contains("newVm.PropertyChanged += OnViewModelPropertyChanged;", loginViewCode, StringComparison.Ordinal);
        Assert.Contains("newVm.ResetCompleted += OnResetCompleted;", loginViewCode, StringComparison.Ordinal);
        Assert.Contains("Unloaded += OnUnloaded;", loginViewCode, StringComparison.Ordinal);
        Assert.Contains("_vm.PropertyChanged -= OnViewModelPropertyChanged;", loginViewCode, StringComparison.Ordinal);
        Assert.Contains("_vm.ResetCompleted -= OnResetCompleted;", loginViewCode, StringComparison.Ordinal);
        Assert.DoesNotContain("newVm.PropertyChanged += (_, e) =>", loginViewCode, StringComparison.Ordinal);
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

