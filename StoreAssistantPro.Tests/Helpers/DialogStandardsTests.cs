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
    public void LoginWindow_Should_Use_OverflowScrollHost_For_MessageGrowth()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginWindow.xaml"));

        Assert.Contains("<ScrollViewer VerticalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{Binding ViewportWidth, RelativeSource={RelativeSource AncestorType=ScrollViewer}}\"", content, StringComparison.Ordinal);
        Assert.Contains("MinHeight=\"{Binding ViewportHeight, RelativeSource={RelativeSource AncestorType=ScrollViewer}}\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("<controls:ViewportConstrainedPanel", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_Keep_FooterOutside_ScrollableBody()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml"));

        Assert.Contains("<Border Grid.Row=\"2\"", content, StringComparison.Ordinal);
        Assert.Contains("<ScrollViewer VerticalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("<Border Grid.Row=\"6\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SaleHistoryWindow_Should_Allow_Horizontal_ReceiptPreview_Scrolling()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryWindow.xaml"));

        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("PanningMode=\"Both\"", content, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"NoWrap\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("h:SmoothScroll.IsEnabled=\"True\"", content, StringComparison.Ordinal);
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
    public void DenseDialogForms_Should_Allow_Horizontal_Overflow_Recovery()
    {
        var files = new[]
        {
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementWindow.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmWindow.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryWindow.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementWindow.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementWindow.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsWindow.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementWindow.xaml")
        };

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
            Assert.Contains("PanningMode=\"Both\"", content, StringComparison.Ordinal);
            Assert.DoesNotContain("PanningMode=\"VerticalOnly\"", content, StringComparison.Ordinal);
            Assert.DoesNotContain("h:SmoothScroll.IsEnabled=\"True\"", content, StringComparison.Ordinal);
        }
    }
    [Fact]
    public void DenseAndOperationalDialogs_Should_Allow_Resize()
    {
        var files = new[]
        {
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "FinancialYears", "Views", "FinancialYearWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Users", "Views", "UsersWindow.xaml.cs"),
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementWindow.xaml.cs")
        };

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.Contains("protected override bool AllowResize => true;", content, StringComparison.Ordinal);
            Assert.Contains("protected override double DialogMinWidth =>", content, StringComparison.Ordinal);
            Assert.Contains("protected override double DialogMinHeight =>", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TopLevelWindowIcons_Should_Be_Applied_In_CodeBehind()
    {
        var mainWindowXaml = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var loginWindowXaml = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginWindow.xaml"));
        var setupWindowXaml = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml"));
        var mainWindowCode = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));
        var loginWindowCode = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginWindow.xaml.cs"));
        var setupWindowCode = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml.cs"));

        Assert.DoesNotContain("Icon=\"pack://application:,,,/Assets/app.ico\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"pack://application:,,,/Assets/app.ico\"", loginWindowXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"pack://application:,,,/Assets/app.ico\"", setupWindowXaml, StringComparison.Ordinal);
        Assert.Contains("WindowIconHelper.Apply(this);", mainWindowCode, StringComparison.Ordinal);
        Assert.Contains("WindowIconHelper.Apply(this);", loginWindowCode, StringComparison.Ordinal);
        Assert.Contains("WindowIconHelper.Apply(this);", setupWindowCode, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginWindow_Should_Unsubscribe_ViewModel_Handlers_On_Close()
    {
        var loginWindowCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginWindow.xaml.cs"));

        Assert.Contains("vm.PropertyChanged += OnViewModelPropertyChanged;", loginWindowCode, StringComparison.Ordinal);
        Assert.Contains("vm.ResetCompleted += OnResetCompleted;", loginWindowCode, StringComparison.Ordinal);
        Assert.Contains("Closed += OnClosed;", loginWindowCode, StringComparison.Ordinal);
        Assert.Contains("_vm.PropertyChanged -= OnViewModelPropertyChanged;", loginWindowCode, StringComparison.Ordinal);
        Assert.Contains("_vm.ResetCompleted -= OnResetCompleted;", loginWindowCode, StringComparison.Ordinal);
        Assert.DoesNotContain("vm.PropertyChanged += (_, e) =>", loginWindowCode, StringComparison.Ordinal);
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

