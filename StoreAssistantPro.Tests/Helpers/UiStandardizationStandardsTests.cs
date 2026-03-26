using Xunit;
using System.Text.RegularExpressions;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class UiStandardizationStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedStyleLayer_Should_Define_Action_Report_And_Overlay_Standardization_Tokens()
    {
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Thickness x:Key=\"CommandPaletteItemPadding\">14,12</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"OverlayMenuItemPadding\">12,10</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"OverlayMenuItemGap\">0,0,0,4</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"WorkspaceHeaderStatusCardPadding\">16,12</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"WorkspaceHeaderStatusStackGap\">12,0,0,0</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"WorkspaceHeroAccentLeftMargin\">-32,-24,0,0</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"WorkspaceHeroAccentRightMargin\">0,-8,36,0</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"SettingsSectionHeaderRowMargin\">16,16,16,12</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"SettingsSectionRowMargin\">16,12</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"SettingsSectionFooterRowMargin\">16,12,16,16</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"SettingsSectionDividerMargin\">16,0</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"ReportMetricTilePadding\">10,6</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"ReportMetricTileMargin\">0,0,8,4</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SettingsFieldColumnWidth\">192</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ReportDateRangeWidth\">344</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"QuickActionOverflowButtonSize\">40</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentLeftWidth\">320</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentLeftHeight\">176</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentLeftCornerRadius\">160</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentRightWidth\">220</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentRightHeight\">136</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentRightCornerRadius\">110</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"MasterDetailListPaneWidth\">3*</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"MasterDetailPaneGapWidth\">12</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"MasterDetailEditorPaneWidth\">2*</GridLength>", designSystem, StringComparison.Ordinal);

        Assert.Contains("<Style x:Key=\"PageSubtitleStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"CancelButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"CloseButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DeleteButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PrintButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"UtilityButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"FormActionRowStyle\" TargetType=\"StackPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"RequiredFieldIndicatorRunStyle\" TargetType=\"Run\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AdminSectionCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AdminSectionHighlightCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AdminSectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AdminSectionDescriptionStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AdminInlineFieldRowStyle\" TargetType=\"Grid\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PathPickerRowStyle\" TargetType=\"Grid\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PathPickerBrowseButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"WarningCalloutCardStyle\" TargetType=\"Border\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"WarningCalloutIconStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"WarningCalloutTitleTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"WarningCalloutBodyTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"EntityLookupValueTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"EntityLookupClearButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SearchSuggestionCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SearchSuggestionPrimaryTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SearchSuggestionSecondaryTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryStatCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryStatLabelTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryStatValueTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryPanelStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryBannerStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryPanelTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryKeyTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryBannerLabelTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryValueTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryAccentValueTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryHeadlineValueTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryBannerValueTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SummaryMetaTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InlineMoneyActionPanelStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InlineMoneyActionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InlineMoneyActionCommandRowStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InlineMoneyActionPrimaryButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PaginatorBarStyle\" TargetType=\"StackPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PaginatorRightAlignedBarStyle\" TargetType=\"StackPanel\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PaginatorNavButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PaginatorInfoTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"OperationalEditorSectionCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"OperationalEditorSectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"OperationalEditorSectionDescriptionStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"LineItemActionRowStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InlineCollectionAddButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InlineCollectionRemoveButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StateTransitionActionBarStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StateTransitionPrimaryButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StateTransitionSecondaryButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StateTransitionDestructiveButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSectionCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSectionMetaTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportMetricTileStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportMetricLabelTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportMetricValueTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportExportButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSubsectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportListPrimaryTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportListSecondaryTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportListValueTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportListMetaTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsView_Should_Use_Shared_Report_Styles_And_Responsive_Filter_Bar()
    {
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));

        Assert.Contains("Width=\"{StaticResource ReportDateRangeWidth}\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"344\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportSectionCardStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportSectionTitleStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportMetricTileStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportMetricLabelTextStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportMetricValueTextStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportSectionMetaTextStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportExportButtonStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportSubsectionTitleStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportListPrimaryTextStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportListSecondaryTextStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportListValueTextStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportListMetaTextStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Padding=\"16\" Margin=\"0,0,0,12\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Padding=\"10,6\" Margin=\"0,0,8,4\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"16\" FontWeight=\"Bold\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource ToolbarButtonStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"13\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"12\"", reportsView, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemSettings_Should_Use_Tokenized_Responsive_Field_Widths_And_Shared_Save_Action()
    {
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));

        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"192\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource SettingsFieldColumnWidth}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SegmentedFilterHostStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource SettingsSectionHeaderRowMargin}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource SettingsSectionRowMargin}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource SettingsSectionFooterRowMargin}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource SettingsSectionDividerMargin}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutCardStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutTitleTextStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutBodyTextStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"16,16,16,12\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"16,12,16,16\"", settingsView, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_Header_And_Hero_Backdrop_Should_Use_Shared_Layout_Tokens()
    {
        var workspaceView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml"));

        Assert.Contains("Width=\"{StaticResource WorkspaceHeroAccentLeftWidth}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Height=\"{StaticResource WorkspaceHeroAccentLeftHeight}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource WorkspaceHeroAccentLeftMargin}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("CornerRadius=\"{StaticResource WorkspaceHeroAccentLeftCornerRadius}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource WorkspaceHeroAccentRightWidth}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Height=\"{StaticResource WorkspaceHeroAccentRightHeight}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource WorkspaceHeroAccentRightMargin}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("CornerRadius=\"{StaticResource WorkspaceHeroAccentRightCornerRadius}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Padding=\"{StaticResource WorkspaceHeaderStatusCardPadding}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource WorkspaceHeaderStatusStackGap}\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"320\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"220\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Padding=\"12,10\"", workspaceView, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_Overlays_Should_Use_Shared_Density_Tokens()
    {
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("Padding=\"{StaticResource CommandPaletteItemPadding}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Width\" Value=\"{StaticResource QuickActionOverflowButtonSize}\"/>", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Height\" Value=\"{StaticResource QuickActionOverflowButtonSize}\"/>", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"{StaticResource QuickActionOverflowButtonSize}\"/>", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Padding\" Value=\"{StaticResource OverlayMenuItemPadding}\"/>", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Margin\" Value=\"{StaticResource OverlayMenuItemGap}\"/>", mainWindow, StringComparison.Ordinal);

        Assert.DoesNotContain("Padding=\"14,12\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Padding=\"12,10\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("<Setter Property=\"Width\" Value=\"40\"/>", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("<Setter Property=\"Height\" Value=\"40\"/>", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("<Setter Property=\"MinHeight\" Value=\"40\"/>", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_Pages_Should_Use_Shared_Action_Taxonomy_Styles()
    {
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var financialYearView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "FinancialYears", "Views", "FinancialYearView.xaml"));
        var taxView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"));
        var variantView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"));
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));
        var inventoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"));
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var inwardView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));

        Assert.Contains("Content=\"Browse...\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource UtilityButtonStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Refresh\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource RefreshButtonStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Verify\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Yes, Restore\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Cancel\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CancelButtonStyle}\"", backupView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Confirm Reset\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Reset Billing\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Create Next Year\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource NewButtonStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CancelButtonStyle}\"", financialYearView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Delete\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", variantView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Cancel Bill\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Remove Item\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CancelButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", billingView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Delete\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Delete Slab\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Save Group\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Save HSN\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource UtilityButtonStyle}\"", taxView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Cancel Purchase Order\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionDestructiveButtonStyle}\"", purchaseOrderView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Cancel GRN\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionDestructiveButtonStyle}\"", grnView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Cancel Stock Take\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", inventoryView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Close\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CloseButtonStyle}\"", vendorView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Cancel\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CancelButtonStyle}\"", loginView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Save\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", firmView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Save\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", inwardView, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_Forms_Should_Not_Rely_On_Horizontal_Form_Scrolling()
    {
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var inwardView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var variantView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"));

        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", loginView, StringComparison.Ordinal);

        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", customerView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", customerView, StringComparison.Ordinal);

        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", vendorView, StringComparison.Ordinal);

        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", firmView, StringComparison.Ordinal);

        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", inwardView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", inwardView, StringComparison.Ordinal);

        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", productView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", productView, StringComparison.Ordinal);

        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", variantView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", variantView, StringComparison.Ordinal);
    }

    [Fact]
    public void Active_Filter_Chip_Rows_And_Shell_Notifications_Should_Not_Use_Horizontal_Scroll_Fallbacks()
    {
        var branchView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"));
        var debtorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"));
        var expenseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        var paymentView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var orderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var salaryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var salesPurchaseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));
        var ironingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"));
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("<WrapPanel Margin=\"0,8,0,0\">", branchView, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", branchView, StringComparison.Ordinal);

        Assert.Contains("<WrapPanel Margin=\"0,8,0,0\">", debtorView, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", debtorView, StringComparison.Ordinal);

        Assert.Contains("<WrapPanel Margin=\"0,8,0,0\">", expenseView, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", expenseView, StringComparison.Ordinal);

        Assert.Contains("<WrapPanel Margin=\"0,8,0,0\">", paymentView, StringComparison.Ordinal);
        Assert.Contains("StringFormat=Date: {0}", paymentView, StringComparison.Ordinal);

        Assert.Contains("<WrapPanel Margin=\"0,8,0,0\">", orderView, StringComparison.Ordinal);
        Assert.Contains("StringFormat=Status: {0}", orderView, StringComparison.Ordinal);

        Assert.Contains("<WrapPanel Margin=\"0,8,0,0\">", salaryView, StringComparison.Ordinal);
        Assert.Contains("StringFormat=Status: {0}", salaryView, StringComparison.Ordinal);

        Assert.Contains("<WrapPanel Margin=\"0,8,0,0\">", salesPurchaseView, StringComparison.Ordinal);
        Assert.Contains("StringFormat=Date: {0}", salesPurchaseView, StringComparison.Ordinal);
        Assert.Contains("StringFormat=Type: {0}", salesPurchaseView, StringComparison.Ordinal);

        Assert.Contains("<WrapPanel Margin=\"0,8,0,0\">", ironingView, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", ironingView, StringComparison.Ordinal);

        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("ScrollViewer.HorizontalScrollBarVisibility=\"Auto\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_Summary_Cards_Should_Use_Shared_Stat_Card_Styles()
    {
        var files = new[]
        {
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"),
        };

        foreach (var file in files)
        {
            var xaml = File.ReadAllText(file);
            Assert.Contains("Style=\"{StaticResource SummaryStatCardStyle}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource SummaryStatLabelTextStyle}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource SummaryStatValueTextStyle}\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Padding=\"12,8\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("FontSize=\"18\" FontWeight=\"Bold\"", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Crud_Form_Command_Rows_Should_Use_Shared_Form_Action_Row_And_Action_Styles()
    {
        var files = new[]
        {
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"),
        };

        foreach (var file in files)
        {
            var xaml = File.ReadAllText(file);
            Assert.Contains("Style=\"{StaticResource FormActionRowStyle}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource UtilityButtonStyle}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Representative_Forms_Should_Use_Shared_Required_Field_Indicator_Style()
    {
        var files = new[]
        {
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"),
        };

        foreach (var file in files)
        {
            var xaml = File.ReadAllText(file);
            Assert.Contains("Style=\"{StaticResource RequiredFieldIndicatorRunStyle}\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("<Run Text=\"*\" Foreground=\"{StaticResource FluentError}\"/>", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Representative_Utility_Commands_Should_Use_Shared_Utility_Styles()
    {
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var inventoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var barcodeView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "BarcodeLabels", "Views", "BarcodeLabelView.xaml"));
        var normalizedInventoryView = inventoryView.Replace("\r\n", "\n", StringComparison.Ordinal);
        var normalizedVendorView = vendorView.Replace("\r\n", "\n", StringComparison.Ordinal);
        var normalizedBarcodeView = barcodeView.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("Content=\"Browse...\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PathPickerBrowseButtonStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Refresh\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource RefreshButtonStyle}\"", backupView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Import CSV\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Export CSV\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"View History\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Import CSV\" Command=\"{Binding ImportCsvCommand}\"\n                        Style=\"{StaticResource UtilityButtonStyle}\"", normalizedInventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Export CSV\" Command=\"{Binding ExportCsvCommand}\"\n                        Style=\"{StaticResource UtilityButtonStyle}\"", normalizedInventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"View History\"\n                        Command=\"{Binding LoadMovementHistoryCommand}\"\n                        CommandParameter=\"{Binding SelectedProduct}\"\n                        IsEnabled=\"{Binding HasSelectedProduct}\"\n                        Style=\"{StaticResource UtilityButtonStyle}\"", normalizedInventoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Import CSV\" Command=\"{Binding ImportCsvCommand}\"\n                        Style=\"{StaticResource SecondaryButtonStyle}\"", normalizedInventoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Export CSV\" Command=\"{Binding ExportCsvCommand}\"\n                        Style=\"{StaticResource SecondaryButtonStyle}\"", normalizedInventoryView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Ledger\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Ledger\" Command=\"{Binding LoadLedgerCommand}\"\n                    IsEnabled=\"{Binding IsEditing}\"\n                    Style=\"{StaticResource UtilityButtonStyle}\"", normalizedVendorView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Add All\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Add All\"\n                                Style=\"{StaticResource UtilityButtonStyle}\"\n                                Command=\"{Binding AddAllToBatchCommand}\"", normalizedBarcodeView, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_Pages_Should_Use_Page_Subtitles_For_Header_Guidance()
    {
        var files = new[]
        {
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Users", "Views", "UserManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"),
        };

        foreach (var file in files)
        {
            var xaml = File.ReadAllText(file);
            Assert.Contains("Style=\"{StaticResource PageTitleStyle}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Command_Labels_Should_Use_Ellipsis_Only_For_Follow_Up_Choice_Actions()
    {
        var moduleFiles = Directory.GetFiles(
            Path.Combine(SolutionRoot, "Modules"),
            "*.xaml",
            SearchOption.AllDirectories);

        var ellipsisLabels = moduleFiles
            .SelectMany(file => Regex.Matches(
                    File.ReadAllText(file),
                    "(?:Content|Header)=\"([^\"]*\\.\\.\\.)\"",
                    RegexOptions.CultureInvariant)
                .Cast<Match>()
                .Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Browse...", "Restore..." }, ellipsisLabels);
    }

    [Fact]
    public void Representative_Page_Commands_Should_Use_Standardized_Label_Grammar()
    {
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var inwardView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));

        Assert.Contains("Content=\"New Customer\"", customerView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"New\" Command=\"{Binding NewCustomerCommand}\"", customerView, StringComparison.Ordinal);

        Assert.Contains("Content=\"New Product\"", productView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"New\" Command=\"{Binding NewProductCommand}\"", productView, StringComparison.Ordinal);

        Assert.Contains("Content=\"New Vendor\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"New\" Command=\"{Binding NewVendorCommand}\"", vendorView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Save\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Save Changes\"", firmView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Save\"", inwardView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Save Entry\"", inwardView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Save\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Save Settings\"", settingsView, StringComparison.Ordinal);
    }

    [Fact]
    public void Core_Crud_Pages_Should_Use_Simple_Noun_Based_Titles()
    {
        var titleFiles = new[]
        {
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Users", "Views", "UserManagementView.xaml"),
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"),
        };

        foreach (var file in titleFiles)
        {
            var xaml = File.ReadAllText(file);
            Assert.DoesNotContain(" Management", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource PageTitleStyle}\"", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Representative_Search_Watermarks_Should_Use_Entity_Specific_Grammar()
    {
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));
        var saleHistoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"));
        var branchView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"));
        var brandView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"));
        var categoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"));
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var debtorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"));
        var expenseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));
        var paymentView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var salaryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var salesPurchaseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));

        Assert.Contains("h:Watermark.Text=\"Search customers by name\"", billingView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search products by name\"", billingView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Scan or enter barcode\"", billingView, StringComparison.Ordinal);

        Assert.Contains("h:Watermark.Text=\"Search invoices by invoice number\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search branch bills by bill number or type\"", branchView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search brands by name\"", brandView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search categories by name\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search customers by name or phone\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search debtors by name or phone\"", debtorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search expenses by category or note\"", expenseView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search GRNs by number, PO, or supplier\"", grnView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search payments by customer or note\"", paymentView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search products by name or barcode\"", productView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search purchase orders by number or supplier\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search quotations by number or customer\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search salaries by employee or month\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search sales and purchases by note or type\"", salesPurchaseView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Search vendors by name or GSTIN\"", vendorView, StringComparison.Ordinal);

        Assert.DoesNotContain("h:Watermark.Text=\"e.g. INV-1024\"", saleHistoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. PO-1024 or Sonali Suppliers\"", purchaseOrderView, StringComparison.Ordinal);
    }

    [Fact]
    public void Form_Watermarks_Should_Use_Descriptive_Placeholders_Instead_Of_Example_Copy()
    {
        var moduleXaml = Directory.GetFiles(
            Path.Combine(SolutionRoot, "Modules"),
            "*.xaml",
            SearchOption.AllDirectories);

        foreach (var file in moduleXaml)
        {
            var xaml = File.ReadAllText(file);
            Assert.DoesNotContain("h:Watermark.Text=\"e.g.", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("h:Watermark.Text=\"Optional note\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("h:Watermark.Text=\"Optional notes", xaml, StringComparison.Ordinal);
        }

        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var taxView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"));

        Assert.Contains("h:Watermark.Text=\"Composition rate\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Prefix added before invoice numbers, for example INV-00001.\"", firmView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Customer notes (optional)\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Address line 2 (optional)\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Maximum amount (optional)\"", taxView, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_Status_InfoBars_Should_Use_IsOpen_State_Bindings()
    {
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));
        var cashRegisterView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "CashRegisterView.xaml"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));
        var loginViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "ViewModels", "LoginViewModel.cs"));

        Assert.Contains("IsOpen=\"{Binding HasResetSuccessMessage}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("IsOpen=\"{Binding HasError}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Visibility=\"{Binding ResetSuccessMessage, Converter={StaticResource NonEmptyStringToVisibility}}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Visibility=\"{Binding ErrorMessage, Converter={StaticResource NonEmptyStringToVisibility}}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("[NotifyPropertyChangedFor(nameof(HasResetSuccessMessage))]", loginViewModel, StringComparison.Ordinal);
        Assert.Contains("public bool HasResetSuccessMessage => !string.IsNullOrEmpty(ResetSuccessMessage);", loginViewModel, StringComparison.Ordinal);

        foreach (var xaml in new[] { backupView, billingView, cashRegisterView, firmView, productView, settingsView })
        {
            Assert.Contains("IsOpen=\"{Binding HasError}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("IsOpen=\"{Binding HasSuccess}\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Visibility=\"{Binding ErrorMessage, Converter={StaticResource NonEmptyStringToVisibility}}\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Visibility=\"{Binding SuccessMessage, Converter={StaticResource NonEmptyStringToVisibility}}\"", xaml, StringComparison.Ordinal);
        }

        var moduleXaml = Directory.GetFiles(
            Path.Combine(SolutionRoot, "Modules"),
            "*.xaml",
            SearchOption.AllDirectories);

        foreach (var file in moduleXaml)
        {
            var xaml = File.ReadAllText(file);
            Assert.DoesNotContain("HasErrorMessage", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("HasSuccessMessage", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Representative_Surfaces_Should_Not_Contain_Corrupted_Mojibake_Text()
    {
        var variantView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));

        Assert.Contains("Content=\"Back to Products\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Run Text=\" Variants - \"", variantView, StringComparison.Ordinal);
        Assert.Contains("Header=\"+/- Price\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Text=\"+/- Price\"", variantView, StringComparison.Ordinal);

        Assert.Contains("h:DataGridEmptyState.Icon=\"&#xE7B8;\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Debit\" Binding=\"{Binding Debit, StringFormat={}{0:C}}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Credit\" Binding=\"{Binding Credit, StringFormat={}{0:C}}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Balance\" Binding=\"{Binding Balance, StringFormat={}{0:C}}\"", vendorView, StringComparison.Ordinal);

        Assert.Contains("h:DataGridEmptyState.Icon=\"&#xE8A5;\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Header=\"AMOUNT\" Binding=\"{Binding TotalAmount, StringFormat={}{0:C}}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Amount\"", customerView, StringComparison.Ordinal);

        Assert.Contains("Content=\"×\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Text=\" · \"", billingView, StringComparison.Ordinal);
        Assert.Contains("Text=\"items · \"", billingView, StringComparison.Ordinal);

        foreach (var xaml in new[] { variantView, vendorView, customerView, billingView })
        {
            Assert.DoesNotContain("â", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Â", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("ð", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Backup_And_Financial_Admin_Surfaces_Should_Use_Shared_Admin_And_Warning_Styles()
    {
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var financialYearView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "FinancialYears", "Views", "FinancialYearView.xaml"));

        Assert.Contains("Style=\"{StaticResource AdminSectionCardStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionHighlightCardStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionTitleStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionDescriptionStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminInlineFieldRowStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PathPickerRowStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PathPickerBrowseButtonStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutCardStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutIconStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutTitleTextStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutBodyTextStyle}\"", backupView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"⚠", backupView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"âš ", backupView, StringComparison.Ordinal);
        Assert.DoesNotContain("BorderBrush=\"{StaticResource FluentStrokeDefault}\"", backupView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource AdminSectionHighlightCardStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionTitleStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionDescriptionStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutCardStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutIconStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutTitleTextStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutBodyTextStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"⚠", financialYearView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"âš ", financialYearView, StringComparison.Ordinal);
    }

    [Fact]
    public void Billing_And_Cash_Register_Should_Use_Shared_Summary_Panel_And_Admin_Card_Styles()
    {
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));
        var cashRegisterView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "CashRegisterView.xaml"));

        Assert.Contains("Style=\"{StaticResource SummaryPanelStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelTitleStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryKeyTextStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryValueTextStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryHeadlineValueTextStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryMetaTextStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionCardStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionTitleStyle}\"", billingView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"{StaticResource FontSizeHeadline}\"", billingView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource AdminSectionCardStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionHighlightCardStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionTitleStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelTitleStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryKeyTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryValueTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryAccentValueTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutCardStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutTitleTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"{StaticResource FontSizeSubtitle}\"", cashRegisterView, StringComparison.Ordinal);
    }

    [Fact]
    public void Utility_Report_And_Purchase_Flow_Pages_Should_Use_Shared_Page_Subtitles()
    {
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var financialYearView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "FinancialYears", "Views", "FinancialYearView.xaml"));
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));
        var cashRegisterView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "CashRegisterView.xaml"));
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", grnView, StringComparison.Ordinal);
    }

    [Fact]
    public void Billing_Customer_Lookup_Should_Use_Shared_Lookup_And_Suggestion_Styles()
    {
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));

        Assert.Contains("Style=\"{StaticResource EntityLookupValueTextStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource EntityLookupClearButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SearchButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SearchSuggestionCardStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SearchSuggestionPrimaryTextStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SearchSuggestionSecondaryTextStyle}\"", billingView, StringComparison.Ordinal);
        Assert.DoesNotContain("BorderThickness=\"1,0,1,1\"", billingView, StringComparison.Ordinal);
    }

    [Fact]
    public void Customer_Balance_And_Payment_Panel_Should_Use_Shared_Summary_And_Money_Action_Styles()
    {
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource RequiredFieldIndicatorRunStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryBannerStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryBannerLabelTextStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryBannerValueTextStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionPanelStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionTitleStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionCommandRowStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionPrimaryButtonStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"Payment reference\"", customerView, StringComparison.Ordinal);
        Assert.DoesNotContain("Foreground=\"{StaticResource FluentError}\"", customerView, StringComparison.Ordinal);
    }

    [Fact]
    public void Financial_Surfaces_Should_Use_Shared_Summary_Cards_And_Currency_Input_Conventions()
    {
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));
        var billingViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "ViewModels", "BillingViewModel.cs"));
        var cashRegisterView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "CashRegisterView.xaml"));
        var cashRegisterViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "ViewModels", "CashRegisterViewModel.cs"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var vendorViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "ViewModels", "VendorManagementViewModel.cs"));

        Assert.Contains("public string CurrencySymbol => _regional.CurrencySymbol;", billingViewModel, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding DataContext.CurrencySymbol, RelativeSource={RelativeSource AncestorType=UserControl}}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", billingView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Cash received\"", billingView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Discount reason\"", billingView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment reference\"", billingView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. 2500\"", billingView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. UPI 835214\"", billingView, StringComparison.Ordinal);

        Assert.Contains("public string CurrencySymbol => regional.CurrencySymbol;", cashRegisterViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("? $\"âš  Cash variance", cashRegisterViewModel, StringComparison.Ordinal);
        Assert.True(
            Regex.Matches(
                cashRegisterView,
                Regex.Escape("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\""),
                RegexOptions.CultureInvariant).Count >= 3);
        Assert.Contains("Content=\"Close\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"Close summary\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CloseButtonStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Discrepancy\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryValueTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryAccentValueTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Run Text=\" - Returns: \"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"âœ•\"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("Run Text=\" Â· Returns: \"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. 5000\"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. 500\"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. â‚¹50 note damaged\"", cashRegisterView, StringComparison.Ordinal);

        Assert.Contains("public string CurrencySymbol => regional.CurrencySymbol;", vendorViewModel, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryStatCardStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryStatLabelTextStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryStatValueTextStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryHeadlineValueTextStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionPanelStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionTitleStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionPrimaryButtonStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment amount\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment reference (optional)\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment notes (optional)\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. 15000\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. CHQ-12345\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource FluentCardStyle}\"", vendorView, StringComparison.Ordinal);
    }

    [Fact]
    public void Barcode_Label_Tool_Should_Use_Shared_Header_Admin_And_Summary_Styles()
    {
        var barcodeView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "BarcodeLabels", "Views", "BarcodeLabelView.xaml"));

        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PrintButtonStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionCardStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionTitleStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionDescriptionStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelTitleStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource UtilityButtonStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource CardStyle}\"", barcodeView, StringComparison.Ordinal);
    }

    [Fact]
    public void Repeated_Paging_Bars_Should_Use_Shared_Paginator_Styles()
    {
        var expenseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource PaginatorRightAlignedBarStyle}\"", expenseView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorNavButtonStyle}\"", expenseView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", expenseView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorBarStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorNavButtonStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource CaptionStyle}\"", quotationView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorBarStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorNavButtonStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", grnView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource CaptionStyle}\"", grnView, StringComparison.Ordinal);
    }

    [Fact]
    public void Purchase_Flow_Editors_Should_Use_Shared_Editor_Line_Item_And_State_Action_Styles()
    {
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionCardStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource LineItemActionRowStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionAddButtonStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionRemoveButtonStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionActionBarStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionPrimaryButtonStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionSecondaryButtonStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionDestructiveButtonStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Create Purchase Order\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Cancel Purchase Order\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailListPaneWidth}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailPaneGapWidth}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailEditorPaneWidth}\"", purchaseOrderView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionCardStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource LineItemActionRowStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionAddButtonStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionRemoveButtonStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionActionBarStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionPrimaryButtonStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionSecondaryButtonStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionDestructiveButtonStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailListPaneWidth}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailPaneGapWidth}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailEditorPaneWidth}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("<controls:InfoBar Message=\"{Binding ErrorMessage}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("<controls:InfoBar Message=\"{Binding SuccessMessage}\"", quotationView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource SuccessMessageStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource ErrorMessageStyle}\"", quotationView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionCardStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource LineItemActionRowStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionActionBarStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionPrimaryButtonStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource StateTransitionDestructiveButtonStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailListPaneWidth}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailPaneGapWidth}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailEditorPaneWidth}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{StaticResource EditableGridMaxHeight}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("<controls:InfoBar Message=\"{Binding ErrorMessage}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("<controls:InfoBar Message=\"{Binding SuccessMessage}\"", grnView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource SuccessMessageStyle}\"", grnView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource ErrorMessageStyle}\"", grnView, StringComparison.Ordinal);
    }

    [Fact]
    public void Purchase_Flow_Pages_Should_Follow_Metadata_Line_Items_Then_State_Action_Sequence()
    {
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));

        Assert.True(
            purchaseOrderView.IndexOf("Text=\"New Purchase Order\"", StringComparison.Ordinal) <
            purchaseOrderView.IndexOf("Text=\"Line Items\"", StringComparison.Ordinal));
        Assert.True(
            purchaseOrderView.IndexOf("Text=\"Line Items\"", StringComparison.Ordinal) <
            purchaseOrderView.IndexOf("Text=\"State Actions\"", StringComparison.Ordinal));

        Assert.True(
            quotationView.IndexOf("Text=\"New Quotation\"", StringComparison.Ordinal) <
            quotationView.IndexOf("Text=\"Line Items\"", StringComparison.Ordinal));
        Assert.True(
            quotationView.IndexOf("Text=\"Line Items\"", StringComparison.Ordinal) <
            quotationView.IndexOf("Text=\"State Actions\"", StringComparison.Ordinal));

        Assert.True(
            grnView.IndexOf("Text=\"Create GRN from Purchase Order\"", StringComparison.Ordinal) <
            grnView.IndexOf("Text=\"Selected GRN\"", StringComparison.Ordinal));
        Assert.True(
            grnView.IndexOf("Text=\"Selected GRN\"", StringComparison.Ordinal) <
            grnView.IndexOf("Content=\"Confirm GRN\"", StringComparison.Ordinal));
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
