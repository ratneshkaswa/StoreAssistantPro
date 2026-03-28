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
        var appXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "App.xaml"));

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
        Assert.Contains("<sys:Double x:Key=\"FieldWidthCompact\">104</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"FieldWidthStandard\">144</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"FieldWidthWide\">250</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"EntityLookupSuggestionMinWidth\">300</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"HeldBillsOverlayMinWidth\">360</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"TransientListOverlayMaxHeight\">400</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"LoginHeroAccentPrimarySize\">760</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"LoginHeroAccentPrimaryMargin\">-220,-180,0,0</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"LoginHeroAccentPrimaryBlurRadius\">120</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"LoginHeroAccentSecondarySize\">540</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"LoginHeroAccentSecondaryMargin\">0,0,-120,-80</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"LoginHeroAccentSecondaryBlurRadius\">96</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"LoginHeroOutlineSize\">520</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"LoginHeroOutlineMargin\">0,60,0,0</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<CornerRadius x:Key=\"LoginHeroOutlineCornerRadius\">260</CornerRadius>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"SettingsFieldColumnWidth\">192</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ReportDateRangeWidth\">344</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"QuickActionOverflowButtonSize\">40</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"QuickActionOverflowMenuWidth\">260</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"CommandPaletteMinWidth\">560</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"CommandPaletteMaxWidth\">720</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"DashboardMetricTileWideMinWidth\">168</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"DashboardMetricTileStandardMinWidth\">156</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"DashboardMetricTileCompactMinWidth\">150</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"DashboardTrendRegionMaxHeight\">280</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentLeftWidth\">320</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentLeftHeight\">176</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<CornerRadius x:Key=\"WorkspaceHeroAccentLeftCornerRadius\">160</CornerRadius>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentRightWidth\">220</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"WorkspaceHeroAccentRightHeight\">136</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<CornerRadius x:Key=\"WorkspaceHeroAccentRightCornerRadius\">110</CornerRadius>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"MasterDetailListPaneWidth\">3*</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"MasterDetailPaneGapWidth\">12</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"MasterDetailEditorPaneWidth\">2*</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ReadOnlyInspectionGridMaxHeight\">240</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SubordinateDetailRegionMaxHeight\">320</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"CompactToolPaneGapWidth\">16</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"BarcodeToolQueuePaneWidth\">320</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<GridLength x:Key=\"BarcodeToolTemplatePaneWidth\">300</GridLength>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"CompactToolPrimaryActionMinWidth\">148</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"InlineQuantityFieldWidth\">56</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ReportAnalyticalGridMaxHeight\">300</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ReportOperationalGridMaxHeight\">200</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateBrandIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateCategoryIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateCustomerIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateExpenseIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateInvoiceIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateIroningIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateOrderIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStatePaymentIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateProductIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStatePurchaseDocumentIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateQuotationIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateSalaryIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateUserIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:String x:Key=\"EmptyStateVendorIconGlyph\">", designSystem, StringComparison.Ordinal);
        Assert.DoesNotMatch("<sys:Double x:Key=\"[^\"]*CornerRadius\">", designSystem);

        Assert.Contains("<Style x:Key=\"PageSubtitleStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"CancelButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"CloseButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DeleteButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ConfirmButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"RunButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"RestoreButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PrintButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"UtilityButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ViewButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AddButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ImportButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ExportButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"NextButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"BackButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"FormActionRowStyle\" TargetType=\"StackPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Margin\" Value=\"{StaticResource FormActionBarSpacing}\"/>", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PageToolbarHostStyle\" TargetType=\"Grid\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PageToolbarContentRowStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PageToolbarActionRowStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SegmentedFilterButtonRowStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SectionCommandBarStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"PageToolbarMetaTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ToolbarIconButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DirtyStateHintTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AnalyticalReportDataGridColumnHeaderStyle\" TargetType=\"DataGridColumnHeader\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AnalyticalReportDataGridStyle\" TargetType=\"DataGrid\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"RequiredFieldIndicatorRunStyle\" TargetType=\"Run\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SectionDividerStyle\" TargetType=\"Border\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SettingsSectionDividerStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
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
        Assert.Contains("<ControlTemplate x:Key=\"InlineValidationErrorTemplate\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate x:Key=\"InlineValidationTrailingChromeErrorTemplate\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<AdornedElementPlaceholder/>", globalStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("<TextBlock Text=\"!\"", globalStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineValidationTextPadding", globalStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineValidationTrailingChromePadding", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ValidationSummaryCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ValidationSummaryTitleTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ValidationSummaryItemTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
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
        Assert.Contains("<Style x:Key=\"DataGridInlineEditHintIconStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"OperationalEditorSectionCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"OperationalEditorSectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"OperationalEditorSectionDescriptionStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReadOnlyInspectionSectionCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReadOnlyInspectionSectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReadOnlyInspectionSectionDescriptionStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"MasterDetailPageGridStyle\" TargetType=\"Grid\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DetailOverlayHostStyle\" TargetType=\"Border\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DetailOverlayHeaderRowStyle\" TargetType=\"Grid\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DetailOverlayTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DetailOverlaySubtitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReadOnlyPreviewSurfaceStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReadOnlyPreviewScrollViewerStyle\" TargetType=\"ScrollViewer\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReadOnlyPreviewTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"LineItemActionRowStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InlineCollectionAddButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InlineCollectionRemoveButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StateTransitionActionBarStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StateTransitionPrimaryButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StateTransitionSecondaryButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"StateTransitionDestructiveButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSectionCardStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportFilterBarStyle\" TargetType=\"WrapPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSummaryStripStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSummaryItemHostStyle\" TargetType=\"StackPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSummaryItemLabelStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSummaryItemValueStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSectionMetaTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportMetricTileStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportMetricLabelTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportMetricValueTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportExportButtonStyle\" TargetType=\"Button\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("BasedOn=\"{StaticResource ExportButtonStyle}\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportSubsectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportListPrimaryTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportListSecondaryTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportListValueTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ReportListMetaTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardMetricMetaTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardSectionTitleStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardSectionDescriptionStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardCountBadgeTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardListPrimaryTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardListAccentTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardListSecondaryTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardHeaderMonogramTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"DashboardHeaderPrimaryTextStyle\" TargetType=\"TextBlock\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellIdentityBadgeStyle\" TargetType=\"Border\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellCompactIdentityBadgeStyle\" TargetType=\"Border\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellIdentityBadgeTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellCompactIdentityBadgeTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ConnectionStatusDotStyle\" TargetType=\"Ellipse\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellConnectionPrimaryTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"ShellConnectionDetailTextStyle\" TargetType=\"TextBlock\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<helpers:NullToAllConverter x:Key=\"NullToAllConverter\"/>", appXaml, StringComparison.Ordinal);
        Assert.Contains("<helpers:NullToCollapsedConverter x:Key=\"NullToCollapsed\"/>", appXaml, StringComparison.Ordinal);
        Assert.Contains("<helpers:PositiveToVisibilityConverter x:Key=\"PositiveToVisibility\"/>", appXaml, StringComparison.Ordinal);

        var formCardStyleIndex = globalStyles.IndexOf("<Style x:Key=\"FormCardStyle\"", StringComparison.Ordinal);
        var adminSectionCardStyleIndex = globalStyles.IndexOf("<Style x:Key=\"AdminSectionCardStyle\"", StringComparison.Ordinal);
        var summaryPanelStyleIndex = globalStyles.IndexOf("<Style x:Key=\"SummaryPanelStyle\"", StringComparison.Ordinal);
        var hintTextStyleIndex = globalStyles.IndexOf("<Style x:Key=\"HintTextStyle\"", StringComparison.Ordinal);
        var firstHintBasedOnIndex = globalStyles.IndexOf("BasedOn=\"{StaticResource HintTextStyle}\"", StringComparison.Ordinal);
        Assert.True(formCardStyleIndex >= 0, "GlobalStyles.xaml should define FormCardStyle.");
        Assert.True(adminSectionCardStyleIndex > formCardStyleIndex,
            "AdminSectionCardStyle must be declared after FormCardStyle to avoid XAML parse failures.");
        Assert.True(summaryPanelStyleIndex > formCardStyleIndex,
            "SummaryPanelStyle must be declared after FormCardStyle to avoid runtime resource resolution failures.");
        Assert.True(hintTextStyleIndex >= 0, "GlobalStyles.xaml should define HintTextStyle.");
        Assert.True(firstHintBasedOnIndex > hintTextStyleIndex,
            "HintTextStyle must be declared before any style bases on it to avoid runtime resource resolution failures.");

        Assert.Contains("<Style x:Key=\"CountBadgePillStyle\" TargetType=\"Border\"", File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml")), StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InfoBadgePillStyle\" TargetType=\"Border\"", File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml")), StringComparison.Ordinal);
    }

    [Fact]
    public void LoginView_Should_Not_Show_A_Blocking_Verification_Overlay()
    {
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));

        Assert.DoesNotContain("<controls:LoadingOverlay IsActive=\"{Binding IsWorking}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("<controls:ProgressRing IsActive=\"{Binding IsWorking}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Please wait a moment.", loginView, StringComparison.Ordinal);
    }

    [Fact]
    public void InfoBar_Template_Should_Render_NonSuccess_Statuses_As_MessageOnly_Feedback()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Border x:Name=\"Root\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("Background=\"Transparent\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"Transparent\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MessageText\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"TitleText\" Property=\"Visibility\" Value=\"Collapsed\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"Root\" Property=\"BorderThickness\" Value=\"0\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"Root\" Property=\"Background\" Value=\"{StaticResource InfoBarSuccessFill}\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"Root\" Property=\"BorderBrush\" Value=\"{StaticResource InfoBarSuccessBorder}\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"MessageText\" Property=\"Foreground\" Value=\"{StaticResource FluentWarning}\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"MessageText\" Property=\"Foreground\" Value=\"{StaticResource FluentError}\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"MessageText\" Property=\"Foreground\" Value=\"{StaticResource FluentAccentDefault}\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"MessageText\" Property=\"Foreground\" Value=\"{StaticResource FluentSuccess}\"/>", fluentTheme, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalStyles_Should_Not_Use_Forward_BasedOn_StaticResource_Style_References()
    {
        var globalStyles = File.ReadAllLines(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        var styleDefinitionLines = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < globalStyles.Length; index++)
        {
            var match = Regex.Match(globalStyles[index], "<Style x:Key=\"([^\"]+)\"");
            if (match.Success)
            {
                styleDefinitionLines[match.Groups[1].Value] = index + 1;
            }
        }

        var failures = new List<string>();
        for (var index = 0; index < globalStyles.Length; index++)
        {
            var basedOnMatch = Regex.Match(globalStyles[index], "BasedOn=\"\\{StaticResource ([^}]+)\\}\"");
            if (!basedOnMatch.Success)
            {
                continue;
            }

            string? currentStyle = null;
            for (var searchIndex = index; searchIndex >= 0; searchIndex--)
            {
                var styleMatch = Regex.Match(globalStyles[searchIndex], "<Style x:Key=\"([^\"]+)\"");
                if (styleMatch.Success)
                {
                    currentStyle = styleMatch.Groups[1].Value;
                    break;
                }
            }

            if (currentStyle is null)
            {
                continue;
            }

            var targetStyle = basedOnMatch.Groups[1].Value;
            if (styleDefinitionLines.TryGetValue(targetStyle, out var targetLine) && targetLine > index + 1)
            {
                failures.Add($"{currentStyle} line {index + 1} -> {targetStyle} line {targetLine}");
            }
        }

        Assert.True(failures.Count == 0,
            "GlobalStyles.xaml should not forward-reference later style definitions via BasedOn. " +
            string.Join("; ", failures));
    }

    [Fact]
    public void ReportsView_Should_Use_Shared_Report_Styles_And_Responsive_Filter_Bar()
    {
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));

        Assert.Contains("Style=\"{StaticResource ReportFilterBarStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource ReportDateRangeWidth}\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"344\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportSummaryStripStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportSummaryItemHostStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportSummaryItemLabelStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportSummaryItemValueStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SelectedPeriodSummary}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SelectedPresetSummary}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding LastUpdatedSummary}\"", reportsView, StringComparison.Ordinal);
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
        Assert.Contains("MaxHeight=\"{StaticResource ReportAnalyticalGridMaxHeight}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{StaticResource ReportOperationalGridMaxHeight}\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Padding=\"16\" Margin=\"0,0,0,12\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Padding=\"10,6\" Margin=\"0,0,8,4\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"16\" FontWeight=\"Bold\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource ToolbarButtonStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"13\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"12\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"300\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"200\"", reportsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Today's Sales\"", reportsView, StringComparison.Ordinal);
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
        Assert.Contains("Style=\"{StaticResource SummaryPanelStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelTitleStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Current workstation summary\"\r\n                                           Style=\"{StaticResource SectionHeaderStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Current experience summary\"\r\n                                           Style=\"{StaticResource SectionHeaderStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SettingsSectionDividerStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SectionDividerStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutCardStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutTitleTextStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WarningCalloutBodyTextStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"16,16,16,12\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"16,12,16,16\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"1\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Background=\"{StaticResource DividerStrokeColorDefault}\"", settingsView, StringComparison.Ordinal);
    }

    [Fact]
    public void Backup_And_Financial_Year_Admin_Surfaces_Should_Use_Shared_Inline_Help_Patterns()
    {
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var financialYearView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "FinancialYears", "Views", "FinancialYearView.xaml"));

        Assert.Contains("Style=\"{StaticResource InlineHelpButtonStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("h:HelpHint.HelpText=", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineHelpButtonStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("h:HelpHint.HelpText=", financialYearView, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_Forms_Should_Use_Shared_Field_Width_Classes()
    {
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var barcodeView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "BarcodeLabels", "Views", "BarcodeLabelView.xaml"));

        Assert.Contains("Width=\"{StaticResource FieldWidthStandard}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthWide}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthCompact}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthStandard}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthWide}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthCompact}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthStandard}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthWide}\"", productView, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{StaticResource FieldWidthWide}\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthCompact}\"", barcodeView, StringComparison.Ordinal);
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
        Assert.Contains("Style=\"{StaticResource ShellIdentityBadgeStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellIdentityBadgeTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardHeaderPrimaryTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ConnectionStatusDotStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellConnectionPrimaryTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellConnectionDetailTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryStatCardStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryStatLabelTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryStatValueTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardMetricMetaTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardSectionTitleStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardSectionDescriptionStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardCountBadgeTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardListPrimaryTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardListAccentTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DashboardListSecondaryTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{StaticResource DashboardMetricTileWideMinWidth}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{StaticResource DashboardMetricTileStandardMinWidth}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{StaticResource DashboardMetricTileCompactMinWidth}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{StaticResource DashboardTrendRegionMaxHeight}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Icon=\"{StaticResource EmptyStateInvoiceIconGlyph}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Icon=\"{StaticResource EmptyStateProductIconGlyph}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Icon=\"{StaticResource EmptyStateCalendarIconGlyph}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding DataContext.OpenReportsCommand, RelativeSource={RelativeSource AncestorType=Window}}\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"320\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"220\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"168\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"156\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"150\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"280\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Padding=\"12,10\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("FontWeight=\"SemiBold\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Top Products\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Payment Methods\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"📄\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"🔥\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"📦\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"📊\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"💳\"", workspaceView, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_Status_And_Notification_Badges_Should_Use_Shared_Semantic_Styles()
    {
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var workspaceView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml"));
        var printPreview = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml"));

        Assert.Contains("Style=\"{StaticResource ShellCompactIdentityBadgeStyle}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellCompactIdentityBadgeTextStyle}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ConnectionStatusDotStyle}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellConnectionPrimaryTextStyle}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellConnectionDetailTextStyle}\"", mainWindow, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(mainWindow, "NotificationBadgeBehavior\\.DotOnly=\"True\"").Count);
        Assert.DoesNotContain("<Ellipse Width=\"8\" Height=\"8\" VerticalAlignment=\"Center\">", mainWindow, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource ShellIdentityBadgeStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellIdentityBadgeTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ConnectionStatusDotStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellConnectionPrimaryTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ShellConnectionDetailTextStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(workspaceView, "Style=\\\"\\{StaticResource CountBadgePillStyle\\}\\\"").Count);
        Assert.DoesNotContain("Style=\"{StaticResource StatusBadgePillStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("<Ellipse Width=\"8\" Height=\"8\" VerticalAlignment=\"Center\">", workspaceView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource InfoBadgePillStyle}\"", printPreview, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource StatusBadgePillStyle}\"", printPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void Transaction_Row_Context_Menus_Should_Render_Shared_Icons_And_Consistent_Action_Grammar()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var branchView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"));
        var debtorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"));
        var expenseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        var orderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var paymentView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var ironingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"));
        var salaryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var salesPurchaseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));

        Assert.Contains("ContentSource=\"Icon\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("ContentSource=\"Header\"", fluentTheme, StringComparison.Ordinal);

        foreach (var xaml in new[]
                 {
                     branchView, debtorView, expenseView, orderView,
                     paymentView, ironingView, salaryView, salesPurchaseView
                 })
        {
            Assert.Contains("<DataGrid.ContextMenu>", xaml, StringComparison.Ordinal);
            Assert.Contains("<MenuItem Header=\"Edit\"", xaml, StringComparison.Ordinal);
            Assert.Contains("<MenuItem Header=\"Delete\"", xaml, StringComparison.Ordinal);
            Assert.Contains("<MenuItem.Icon>", xaml, StringComparison.Ordinal);
            Assert.Contains("Text=\"&#xE70F;\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Text=\"&#xE74D;\"", xaml, StringComparison.Ordinal);
        }

        Assert.Contains("<Separator/>", branchView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Mark Cleared\"", branchView, StringComparison.Ordinal);
        Assert.Contains("<Separator/>", orderView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Mark Delivered\"", orderView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Mark Pending\"", orderView, StringComparison.Ordinal);
        Assert.Contains("<Separator/>", ironingView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Mark Paid\"", ironingView, StringComparison.Ordinal);
        Assert.Contains("<Separator/>", salaryView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Mark Paid\"", salaryView, StringComparison.Ordinal);
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
        Assert.Contains("Width=\"{StaticResource QuickActionOverflowMenuWidth}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{StaticResource CommandPaletteMinWidth}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MaxWidth=\"{StaticResource CommandPaletteMaxWidth}\"", mainWindow, StringComparison.Ordinal);

        Assert.DoesNotContain("Padding=\"14,12\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Padding=\"12,10\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("<Setter Property=\"Width\" Value=\"40\"/>", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("<Setter Property=\"Height\" Value=\"40\"/>", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("<Setter Property=\"MinHeight\" Value=\"40\"/>", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"260\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"560\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxWidth=\"720\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainShell_Navigation_Labels_Should_Match_Simplified_Page_Vocabulary()
    {
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var mainViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));

        Assert.Contains("Header=\"Firm\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Header=\"Users\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Header=\"Tax\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Header=\"Vendors\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Header=\"Products\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Header=\"Categories\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Header=\"Brands\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Header=\"Customers\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Header=\"Branches\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Firm Management\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"User Management\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Tax Management\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Vendor Management\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Product Management\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Category Management\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Brand Management\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Customer Management\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Branch Bills\"", mainWindow, StringComparison.Ordinal);

        Assert.Contains("NavigateToPage(FirmManagementPage, \"Firm\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(UserManagementPage, \"Users\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(TaxManagementPage, \"Tax\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(VendorManagementPage, \"Vendors\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(ProductManagementPage, \"Products\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(CategoryManagementPage, \"Categories\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(BrandManagementPage, \"Brands\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(InventoryPage, \"Inventory\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(CustomerManagementPage, \"Customers\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(ExpenseManagementPage, \"Expenses\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(DebtorManagementPage, \"Debtors\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(OrderManagementPage, \"Orders\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(IroningManagementPage, \"Ironing\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(SalaryManagementPage, \"Salaries\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(BranchManagementPage, \"Branches\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("NavigateToPage(PaymentManagementPage, \"Payments\")", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("Title = \"Sale History\"", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("Title = \"Purchase Orders\"", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("Title = \"Financial Year\"", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("Title = \"Branches\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Firm management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"User management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Tax management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Vendor management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Product management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Category management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Brand management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Inventory management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Customer management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Expense management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Debtor management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Order management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Ironing management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Salary management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Branch management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Payment management\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("Title = \"PO\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("Title = \"FY\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("Title = \"Sales\"", mainViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("Title = \"Branch\"", mainViewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void MainShell_Quick_Actions_Should_Use_Shared_Fluent_Glyph_Icons()
    {
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var mainViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));
        var quickActionModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Models", "QuickAction.cs"));
        var iconService = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Services", "IconService.cs"));

        Assert.Contains("private static readonly IconService ShellIconService = new();", mainViewModel, StringComparison.Ordinal);

        foreach (var iconName in new[]
                 {
                     "Home", "Firm", "Users", "Tax", "Vendors", "Products", "Categories",
                     "Brands", "Inward", "Inventory", "Billing", "SaleHistory", "Customers",
                     "PurchaseOrders", "FinancialYear", "Settings", "Expenses", "Debtors",
                     "Orders", "Ironing", "Salaries", "Branches", "SalesPurchase",
                     "Payments", "Reports", "BarcodeLabels", "Refresh", "Shortcuts",
                     "CommandPalette", "Search", "Logout"
                 })
        {
            Assert.Contains($"ShellIconService.GetGlyph(\"{iconName}\")", mainViewModel, StringComparison.Ordinal);
            Assert.Contains($"[\"{iconName}\"]", iconService, StringComparison.Ordinal);
        }

        foreach (var emojiIcon in new[]
                 {
                     "🏠", "🏢", "👥", "💰", "📦", "👕", "🏷", "🔖", "📥", "📊", "🛒",
                     "📋", "👤", "📅", "⚙", "💸", "📒", "📝", "👔", "🏬", "💳", "📈",
                     "🔄", "⌨", "⌘", "🔍", "🚪"
                 })
        {
            Assert.DoesNotContain(emojiIcon, mainViewModel, StringComparison.Ordinal);
        }

        Assert.Contains("Text=\"{Binding Icon}\"", posStyles, StringComparison.Ordinal);
        Assert.Contains("FontFamily=\"{StaticResource FluentIconFont}\"", posStyles, StringComparison.Ordinal);

        Assert.Equal(
            4,
            Regex.Matches(
                mainWindow,
                "Text=\"\\{Binding Icon\\}\"\\s*\\r?\\n\\s*FontFamily=\"\\{StaticResource FluentIconFont\\}\"",
                RegexOptions.Singleline).Count);

        Assert.Contains("Text=\"&#xEA8F;\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"ðŸ", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"Loading...\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Loading…\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"Up/Down Navigate   Enter Run   Esc Close\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("↑↓ Navigate   Enter Run   Esc Close", mainWindow, StringComparison.Ordinal);

        Assert.DoesNotContain("Emoji or icon-font glyph", quickActionModel, StringComparison.Ordinal);
        Assert.DoesNotContain("🛒", quickActionModel, StringComparison.Ordinal);
        Assert.DoesNotContain("📦", quickActionModel, StringComparison.Ordinal);
        Assert.DoesNotContain("⚙️", quickActionModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_Pages_Should_Use_Explicit_Centered_And_Compact_Tool_Page_Classes()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));
        var barcodeView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "BarcodeLabels", "Views", "BarcodeLabelView.xaml"));

        Assert.Contains("<Style x:Key=\"CenteredSurfaceCardStyle\" TargetType=\"Border\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"CenteredPageContentHostStyle\" TargetType=\"StackPanel\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SplitToolPageContentGridStyle\" TargetType=\"Grid\">", globalStyles, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource CenteredSurfaceCardStyle}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource LoginHeroAccentPrimarySize}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Height=\"{StaticResource LoginHeroAccentPrimarySize}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource LoginHeroAccentPrimaryMargin}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Radius=\"{StaticResource LoginHeroAccentPrimaryBlurRadius}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource LoginHeroAccentSecondarySize}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Height=\"{StaticResource LoginHeroAccentSecondarySize}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource LoginHeroAccentSecondaryMargin}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Radius=\"{StaticResource LoginHeroAccentSecondaryBlurRadius}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource LoginHeroOutlineSize}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Height=\"{StaticResource LoginHeroOutlineSize}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource LoginHeroOutlineMargin}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("CornerRadius=\"{StaticResource LoginHeroOutlineCornerRadius}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxWidth=\"{StaticResource LoginCardMaxWidth}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"760\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"760\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"-220,-180,0,0\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Radius=\"120\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"540\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"540\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"0,0,-120,-80\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Radius=\"96\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"520\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"520\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"0,60,0,0\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("CornerRadius=\"260\"", loginView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource CenteredPageContentHostStyle}\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxWidth=\"{StaticResource SettingsDialogContentMaxWidth}\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalAlignment=\"Center\"", firmView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource CenteredPageContentHostStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxWidth=\"{StaticResource SettingsDialogContentMaxWidth}\"", settingsView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource SplitToolPageContentGridStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("Grid.Row=\"1\" Margin=\"{StaticResource ItemSpacing}\"", barcodeView, StringComparison.Ordinal);
    }

    [Fact]
    public void Billing_Transient_Overlays_Should_Use_Shared_Overlay_Size_Tokens()
    {
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));

        Assert.Contains("MinWidth=\"{StaticResource EntityLookupSuggestionMinWidth}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{StaticResource HeldBillsOverlayMinWidth}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{StaticResource TransientListOverlayMaxHeight}\"", billingView, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"300\"", billingView, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"360\"", billingView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"400\"", billingView, StringComparison.Ordinal);
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
        Assert.Contains("Content=\"Restore...\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource RestoreButtonStyle}\"", backupView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Reset Billing\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Create Next Year\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource NewButtonStyle}\"", financialYearView, StringComparison.Ordinal);

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

        Assert.True(
            firmView.Contains("Content=\"Save\"", StringComparison.Ordinal)
            || firmView.Contains("Content=\"{Binding SaveButtonText}\"", StringComparison.Ordinal),
            "Firm setup should keep a save action, either as the standard save label or the setup-specific bound label.");
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", firmView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Save\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", inwardView, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_Actions_Should_Use_Semantic_Button_Styles_Instead_Of_Generic_Primary_Secondary()
    {
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));
        var cashRegisterView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "CashRegisterView.xaml"));
        var saleHistoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"));
        var inventoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"));
        var inwardView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));
        var workspaceView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));

        Assert.Contains("Content=\"Reset PIN\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ConfirmButtonStyle}\"", loginView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Backup to USB\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Backup Now\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource RunButtonStyle}\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Restore...\"", backupView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource RestoreButtonStyle}\"", backupView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Add Selected\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AddButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Scan\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource UtilityButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Recall\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Discard\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Add Line\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionAddButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Remove\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionRemoveButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"+ Add Line\"", billingView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Summary\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", cashRegisterView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Preview Receipt\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", saleHistoryView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Variants\"", productView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", productView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Add Row\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionAddButtonStyle}\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Remove\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionRemoveButtonStyle}\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Next &#x2192;\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource NextButtonStyle}\"", inwardView, StringComparison.Ordinal);

        Assert.Contains("Content=\"View\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Import CSV\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ImportButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Export CSV\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ExportButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Save\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", inventoryView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Refresh\"", workspaceView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource RefreshButtonStyle}\"", workspaceView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource ToolbarButtonStyle}\"", workspaceView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Ledger\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", vendorView, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_Print_And_View_Surfaces_Should_Use_Semantic_Command_Styles()
    {
        var barcodeView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "BarcodeLabels", "Views", "BarcodeLabelView.xaml"));
        var saleHistoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"));
        var inventoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"));
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));
        var printPreviewWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml"));

        Assert.Contains("Content=\"Print Labels\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PrintButtonStyle}\"", barcodeView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Preview Receipt\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", saleHistoryView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Import CSV\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ImportButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Export CSV\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ExportButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"View History\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", inventoryView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Export HSN CSV\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReportExportButtonStyle}\"", reportsView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Fit Width\"", printPreviewWindow, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", printPreviewWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Print\"", printPreviewWindow, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PrintButtonStyle}\"", printPreviewWindow, StringComparison.Ordinal);
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
    public void Core_Crud_Form_Command_Rows_Should_Not_Hand_Author_Right_Aligned_Action_Stacks()
    {
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));

        foreach (var xaml in new[] { customerView, vendorView })
        {
            Assert.Contains("Style=\"{StaticResource FormActionRowStyle}\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Orientation=\"Horizontal\" HorizontalAlignment=\"Right\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Margin=\"{StaticResource FormActionBarSpacing}\"", xaml, StringComparison.Ordinal);
        }

        Assert.Contains("Style=\"{StaticResource StickyFooterActionBarStyle}\"", productView, StringComparison.Ordinal);
        Assert.DoesNotContain("Orientation=\"Horizontal\" HorizontalAlignment=\"Right\"", productView, StringComparison.Ordinal);
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

        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        Assert.Contains("Style=\"{StaticResource RequiredFieldIndicatorRunStyle}\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Firm Name *\"", firmView, StringComparison.Ordinal);
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
        Assert.Contains("Command=\"{Binding ImportCsvCommand}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ImportButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ExportCsvCommand}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ExportButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding LoadMovementHistoryCommand}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("CommandParameter=\"{Binding SelectedProduct}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasSelectedProduct}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource SecondaryButtonStyle}\"", normalizedInventoryView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Ledger\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Ledger\" Command=\"{Binding LoadLedgerCommand}\"\n                    IsEnabled=\"{Binding IsEditing}\"\n                    Style=\"{StaticResource ViewButtonStyle}\"", normalizedVendorView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Add All\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Add All\"\n                                Style=\"{StaticResource AddButtonStyle}\"\n                                Command=\"{Binding AddAllToBatchCommand}\"", normalizedBarcodeView, StringComparison.Ordinal);
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

        Assert.True(
            firmView.Contains("Content=\"Save\"", StringComparison.Ordinal)
            || firmView.Contains("Content=\"{Binding SaveButtonText}\"", StringComparison.Ordinal),
            "Firm settings should expose a save action.");
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
    public void Navigation_Back_And_Commit_Actions_Should_Use_Semantic_Button_Styles()
    {
        var variantView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"));
        var inwardView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));
        var cashRegisterView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "CashRegisterView.xaml"));
        var inventoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"));
        var usersView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Users", "Views", "UserManagementView.xaml"));
        var taxView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"));
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));
        var financialYearView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "FinancialYears", "Views", "FinancialYearView.xaml"));

        Assert.Contains("Content=\"Back to Products\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource BackButtonStyle}\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Bulk Create\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource NewButtonStyle}\"", variantView, StringComparison.Ordinal);

        Assert.Contains("Content=\"&#x2190; Back\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource BackButtonStyle}\"", inwardView, StringComparison.Ordinal);

        Assert.Contains("Text=\"Hold Bill\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Recall\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Complete Sale\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource UtilityButtonStyle}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", billingView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Open Register\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Record Movement\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Close Register\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Open Register\" Command=\"{Binding OpenRegisterCommand}\"\r\n                                    Style=\"{StaticResource PrimaryButtonStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Record Movement\" Command=\"{Binding RecordMovementCommand}\"\r\n                                    Style=\"{StaticResource SecondaryButtonStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Close Register\" Command=\"{Binding CloseRegisterCommand}\"\r\n                                    Style=\"{StaticResource PrimaryButtonStyle}\"", cashRegisterView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Apply Adjustment\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Start New Stock Take\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Complete Stock Take\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource NewButtonStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", inventoryView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Change PIN\"", usersView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", usersView, StringComparison.Ordinal);

        Assert.Contains("Content=\"{Binding SlabActionText}\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", taxView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Create Backup\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Restore Backup\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource UtilityButtonStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", settingsView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Set as Current\"", financialYearView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", financialYearView, StringComparison.Ordinal);
    }

    [Fact]
    public void Core_Command_Labels_Should_Map_To_Their_Semantic_Button_Styles()
    {
        var saveFiles = new[]
        {
            "BrandManagementView.xaml",
            "CategoryManagementView.xaml",
            "CustomerManagementView.xaml",
            "FirmManagementView.xaml",
            "InventoryManagementView.xaml",
            "InwardEntryView.xaml",
            "ProductManagementView.xaml",
            "SystemSettingsView.xaml",
            "TaxManagementView.xaml",
            "VariantManagementView.xaml",
            "VendorManagementView.xaml",
        };

        var newFiles = new[]
        {
            "CustomerManagementView.xaml",
            "ProductManagementView.xaml",
            "VendorManagementView.xaml",
        };

        var deleteFiles = new[]
        {
            "TaxManagementView.xaml",
            "VariantManagementView.xaml",
        };

        var refreshFiles = new[]
        {
            "BackupRestoreView.xaml",
            "ReportsView.xaml",
            "WorkspaceView.xaml",
        };

        var printFiles = new[]
        {
            "PrintPreviewWindow.xaml",
        };

        foreach (var file in saveFiles)
        {
            var xaml = File.ReadAllText(FindXaml(file));
            Assert.True(
                xaml.Contains("Content=\"Save\"", StringComparison.Ordinal)
                || (file == "FirmManagementView.xaml" && xaml.Contains("Content=\"{Binding SaveButtonText}\"", StringComparison.Ordinal)),
                $"{file} should expose a save action.");
            Assert.Contains("Style=\"{StaticResource SaveButtonStyle}\"", xaml, StringComparison.Ordinal);
        }

        foreach (var file in newFiles)
        {
            var xaml = File.ReadAllText(FindXaml(file));
            Assert.Contains("Content=\"New ", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource NewButtonStyle}\"", xaml, StringComparison.Ordinal);
        }

        foreach (var file in deleteFiles)
        {
            var xaml = File.ReadAllText(FindXaml(file));
            Assert.Contains("Content=\"Delete\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource DeleteButtonStyle}\"", xaml, StringComparison.Ordinal);
        }

        foreach (var file in refreshFiles)
        {
            var xaml = File.ReadAllText(FindXaml(file));
            Assert.Contains("Content=\"Refresh\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource RefreshButtonStyle}\"", xaml, StringComparison.Ordinal);
        }

        foreach (var file in printFiles)
        {
            var xaml = File.ReadAllText(FindXaml(file));
            Assert.Contains("Content=\"Print\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource PrintButtonStyle}\"", xaml, StringComparison.Ordinal);
        }

        Assert.Equal(saveFiles.Length - 1, CountFilesContaining("Content=\"Save\""));
        Assert.Equal(newFiles.Length, CountFilesContaining("Content=\"New "));
        Assert.Equal(deleteFiles.Length, CountFilesContaining("Content=\"Delete\""));
        Assert.Equal(refreshFiles.Length, CountFilesContaining("Content=\"Refresh\""));
        Assert.Equal(printFiles.Length, CountFilesContaining("Content=\"Print\""));
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
    public void Search_Button_Should_Be_Reserved_For_Explicit_Apply_And_Entity_Lookup_Searches()
    {
        var billingViewPath = Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml");
        var saleHistoryViewPath = Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml");
        var productViewPath = Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml");
        var purchaseOrderViewPath = Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml");
        var grnViewPath = Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml");
        var quotationViewPath = Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml");

        var billingView = File.ReadAllText(billingViewPath);
        var saleHistoryView = File.ReadAllText(saleHistoryViewPath);
        var productView = File.ReadAllText(productViewPath);
        var purchaseOrderView = File.ReadAllText(purchaseOrderViewPath);
        var grnView = File.ReadAllText(grnViewPath);
        var quotationView = File.ReadAllText(quotationViewPath);

        Assert.Contains("Content=\"Search Customers\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding SearchCustomersCommand}\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SearchButtonStyle}\"", billingView, StringComparison.Ordinal);

        foreach (var view in new[] { saleHistoryView, productView, purchaseOrderView, grnView, quotationView })
        {
            Assert.Contains("Content=\"Search\"", view, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource SearchButtonStyle}\"", view, StringComparison.Ordinal);
            Assert.True(
                view.Contains("<DatePicker", StringComparison.Ordinal) ||
                view.Contains("<ComboBox", StringComparison.Ordinal) ||
                view.Contains("DateRangePicker", StringComparison.Ordinal),
                "Explicit-apply search rows must combine text search with another filter control.");
        }

        var filesWithSearchButtons = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(SolutionRoot, "Modules"), "*.xaml", SearchOption.AllDirectories))
        {
            var xaml = File.ReadAllText(file);
            if (xaml.Contains("Content=\"Search\"", StringComparison.Ordinal) ||
                xaml.Contains("Content=\"Search Customers\"", StringComparison.Ordinal))
            {
                filesWithSearchButtons.Add(Path.GetFileName(file));
            }
        }

        Assert.Equal(6, filesWithSearchButtons.Count);
        Assert.Contains("BillingView.xaml", filesWithSearchButtons);
        Assert.Contains("SaleHistoryView.xaml", filesWithSearchButtons);
        Assert.Contains("ProductManagementView.xaml", filesWithSearchButtons);
        Assert.Contains("PurchaseOrderView.xaml", filesWithSearchButtons);
        Assert.Contains("GRNManagementView.xaml", filesWithSearchButtons);
        Assert.Contains("QuotationManagementView.xaml", filesWithSearchButtons);
    }

    [Fact]
    public void Plain_Back_Close_And_Cancel_Labels_Should_Map_To_Their_Semantic_Styles()
    {
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));
        var masterPinDialog = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "MasterPinDialog.xaml"));
        var cashRegisterView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "CashRegisterView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var printPreviewWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml"));
        var inwardView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));
        var variantView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"));

        Assert.Contains("Content=\"Cancel\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CancelButtonStyle}\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Cancel\"", masterPinDialog, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterSecondaryButtonStyle}\"", masterPinDialog, StringComparison.Ordinal);

        Assert.Contains("Content=\"Close\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CloseButtonStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Close\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CloseButtonStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Close\"", printPreviewWindow, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DialogFooterCloseButtonStyle}\"", printPreviewWindow, StringComparison.Ordinal);

        Assert.Contains("Content=\"&#x2190; Back\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource BackButtonStyle}\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Back to Products\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource BackButtonStyle}\"", variantView, StringComparison.Ordinal);

        var filesWithPlainCancel = new List<string>();
        var filesWithPlainClose = new List<string>();
        var filesWithBack = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(SolutionRoot, "Modules"), "*.xaml", SearchOption.AllDirectories)
                                  .Concat(Directory.GetFiles(Path.Combine(SolutionRoot, "Core"), "*.xaml", SearchOption.AllDirectories)))
        {
            var xaml = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);
            if (xaml.Contains("Content=\"Cancel\"", StringComparison.Ordinal))
            {
                filesWithPlainCancel.Add(fileName);
            }

            if (xaml.Contains("Content=\"Close\"", StringComparison.Ordinal))
            {
                filesWithPlainClose.Add(fileName);
            }

            if (xaml.Contains("Content=\"Back to", StringComparison.Ordinal) ||
                xaml.Contains("Content=\"&#x2190; Back\"", StringComparison.Ordinal))
            {
                filesWithBack.Add(fileName);
            }
        }

        Assert.Equal(2, filesWithPlainCancel.Count);
        Assert.Contains("LoginView.xaml", filesWithPlainCancel);
        Assert.Contains("MasterPinDialog.xaml", filesWithPlainCancel);

        Assert.Equal(3, filesWithPlainClose.Count);
        Assert.Contains("CashRegisterView.xaml", filesWithPlainClose);
        Assert.Contains("VendorManagementView.xaml", filesWithPlainClose);
        Assert.Contains("PrintPreviewWindow.xaml", filesWithPlainClose);

        Assert.Equal(2, filesWithBack.Count);
        Assert.Contains("InwardEntryView.xaml", filesWithBack);
        Assert.Contains("VariantManagementView.xaml", filesWithBack);
    }

    [Fact]
    public void Filter_Heavy_Pages_Should_Use_Shared_Toolbar_Host_Content_And_Action_Styles()
    {
        var saleHistoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource PageToolbarHostStyle}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarContentRowStyle}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarActionRowStyle}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ToolbarIconButtonStyle}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"36\"", saleHistoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"36\"", saleHistoryView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PageToolbarHostStyle}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarContentRowStyle}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarActionRowStyle}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarMetaTextStyle}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ToolbarIconButtonStyle}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthWide}\"", productView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"36\"", productView, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"36\"", productView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PageToolbarHostStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarContentRowStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarActionRowStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthWide}\"", purchaseOrderView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PageToolbarHostStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarContentRowStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarActionRowStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthWide}\"", grnView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PageToolbarHostStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarContentRowStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageToolbarActionRowStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthWide}\"", quotationView, StringComparison.Ordinal);
    }

    [Fact]
    public void Segmented_Filters_And_Section_Command_Bars_Should_Use_Shared_Wrap_Overflow_Styles()
    {
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var branchView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"));
        var debtorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"));
        var expenseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        var ironingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"));
        var orderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var paymentView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));
        var salaryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var salesPurchaseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));

        Assert.Contains("Style=\"{StaticResource SectionCommandBarStyle}\"", backupView, StringComparison.Ordinal);
        Assert.DoesNotContain("<StackPanel Orientation=\"Horizontal\" Margin=\"0,12,0,0\">", backupView, StringComparison.Ordinal);

        foreach (var xaml in new[]
                 {
                     branchView, debtorView, expenseView, ironingView, orderView,
                     paymentView, reportsView, salaryView, salesPurchaseView, settingsView
                 })
        {
            Assert.Contains("Style=\"{StaticResource SegmentedFilterButtonRowStyle}\"", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Reports_Should_Use_Shared_Analytical_DataGrid_Styles()
    {
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));

        Assert.Equal(6, Regex.Matches(reportsView, "Style=\\\"\\{StaticResource AnalyticalReportDataGridStyle\\}\\\"").Count);
        Assert.DoesNotContain("Style=\"{StaticResource {x:Type DataGrid}}\"", reportsView, StringComparison.Ordinal);
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
        var debtorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"));
        var ironingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"));
        var ordersView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var salaryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var taxView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"));
        var billingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingView.xaml"));

        Assert.Contains("h:Watermark.Text=\"Composition rate\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Prefix added before invoice numbers, for example INV-00001.\"", firmView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment amount\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment reference or note (optional)\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Customer notes (optional)\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Phone number\"", debtorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Total amount\"", debtorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Amount paid\"", debtorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Quantity\"", ironingView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Rate per item\"", ironingView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Quantity\"", ordersView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Rate per item\"", ordersView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Salary year\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Advance amount\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Days worked\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Days absent\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Hours worked\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Incentive amount\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Address line 2 (optional)\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Maximum amount (optional)\"", taxView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment reference\"", billingView, StringComparison.Ordinal);

        Assert.DoesNotContain("h:Watermark.Text=\"Phone\"", debtorView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Total\"", debtorView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Paid\"", debtorView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Qty\"", ironingView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Rate\"", ironingView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Qty\"", ordersView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Rate\"", ordersView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Year\"", salaryView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Advance\"", salaryView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Days\"", salaryView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Hours\"", salaryView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Amount\"", customerView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Reference or note (optional)\"", customerView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Ref #\"", billingView, StringComparison.Ordinal);
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

        foreach (var xaml in new[] { backupView, billingView, cashRegisterView })
        {
            Assert.Contains("IsOpen=\"{Binding HasError}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("IsOpen=\"{Binding HasSuccess}\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Visibility=\"{Binding ErrorMessage, Converter={StaticResource NonEmptyStringToVisibility}}\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Visibility=\"{Binding SuccessMessage, Converter={StaticResource NonEmptyStringToVisibility}}\"", xaml, StringComparison.Ordinal);
        }

        foreach (var xaml in new[] { firmView, productView, settingsView })
        {
            Assert.Contains("IsOpen=\"{Binding HasNonValidationError}\"", xaml, StringComparison.Ordinal);
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

        Assert.Contains("h:DataGridEmptyState.Icon=\"{StaticResource EmptyStateVendorIconGlyph}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Debit\" Binding=\"{Binding Debit, StringFormat={}{0:C}}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Credit\" Binding=\"{Binding Credit, StringFormat={}{0:C}}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Balance\" Binding=\"{Binding Balance, StringFormat={}{0:C}}\"", vendorView, StringComparison.Ordinal);

        Assert.Contains("h:DataGridEmptyState.Icon=\"{StaticResource EmptyStateCustomerIconGlyph}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Header=\"AMOUNT\" Binding=\"{Binding TotalAmount, StringFormat={}{0:C}}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment amount\"", customerView, StringComparison.Ordinal);

        Assert.Contains("Content=\"Discard\"", billingView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Remove\"", billingView, StringComparison.Ordinal);
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

        Assert.True(
            Regex.Matches(cashRegisterView, "Style=\\\"\\{StaticResource OperationalEditorSectionCardStyle\\}\\\"").Count >= 3,
            "Cash register should use shared operational editor cards for register actions.");
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionCardStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionTitleStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionDescriptionStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryBannerStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelTitleStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryKeyTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryValueTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryAccentValueTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryBannerLabelTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryMetaTextStyle}\"", cashRegisterView, StringComparison.Ordinal);
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
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));
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
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", settingsView, StringComparison.Ordinal);
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
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", customerView, StringComparison.Ordinal);
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
        Assert.Contains("Style=\"{StaticResource SummaryPanelStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelTitleStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryKeyTextStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryValueTextStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryHeadlineValueTextStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionPanelStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionTitleStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineMoneyActionPrimaryButtonStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment amount\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment reference (optional)\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Payment notes (optional)\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource SummaryStatCardStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. 15000\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"e.g. CHQ-12345\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource FluentCardStyle}\"", vendorView, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_Money_Editors_Should_Use_Currency_Prefix_And_Decimal_Input_Constraints()
    {
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var salesPurchaseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));

        Assert.Contains("AutomationProperties.Name=\"Payment amount\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.Scope=\"Number\"", customerView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", customerView, StringComparison.Ordinal);

        Assert.Contains("AutomationProperties.Name=\"Sale price\"", productView, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"Cost price\"", productView, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(productView, "h:NumericInput\\.IsDecimalOnly=\"True\"").Count);
        Assert.Equal(2, Regex.Matches(productView, Regex.Escape("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"")).Count);

        Assert.Contains("AutomationProperties.Name=\"Amount\"", salesPurchaseView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", salesPurchaseView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.Scope=\"Number\"", salesPurchaseView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", salesPurchaseView, StringComparison.Ordinal);
    }

    [Fact]
    public void Barcode_Label_Tool_Should_Use_Shared_Header_Admin_And_Summary_Styles()
    {
        var barcodeView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "BarcodeLabels", "Views", "BarcodeLabelView.xaml"));

        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AddButtonStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PrintButtonStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionCardStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionTitleStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AdminSectionDescriptionStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource SummaryPanelTitleStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource UtilityButtonStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineCollectionRemoveButtonStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Icon=\"{StaticResource EmptyStateProductIconGlyph}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Icon=\"{StaticResource EmptyStatePurchaseDocumentIconGlyph}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource BarcodeToolQueuePaneWidth}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource BarcodeToolTemplatePaneWidth}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"{StaticResource CompactToolPrimaryActionMinWidth}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource InlineQuantityFieldWidth}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource FieldWidthCompact}\"", barcodeView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FormRowLabelStyle}\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"📦\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"🏷\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"320\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"148\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"300\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"48\"", barcodeView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource CardStyle}\"", barcodeView, StringComparison.Ordinal);
    }

    [Fact]
    public void Repeated_Paging_Bars_Should_Use_Shared_Paginator_Styles()
    {
        var brandView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"));
        var categoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"));
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var expenseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        var saleHistoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"));
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource PaginatorRightAlignedBarStyle}\"", brandView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding PreviousPageCommand}\"", brandView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding NextPageCommand}\"", brandView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", brandView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", brandView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", brandView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorRightAlignedBarStyle}\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding PreviousPageCommand}\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding NextPageCommand}\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", categoryView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorBarStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding PreviousPageCommand}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding NextPageCommand}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", customerView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorRightAlignedBarStyle}\"", expenseView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorNavButtonStyle}\"", expenseView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", expenseView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", expenseView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", expenseView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorBarStyle}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding PreviousPageCommand}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding NextPageCommand}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", saleHistoryView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorBarStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding PreviousPageCommand}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding NextPageCommand}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", purchaseOrderView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorBarStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorNavButtonStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", quotationView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource CaptionStyle}\"", quotationView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorBarStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorNavButtonStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", grnView, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"{StaticResource CaptionStyle}\"", grnView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource PaginatorBarStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding PreviousPageCommand}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding NextPageCommand}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasPreviousPage}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasNextPage}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PaginatorInfoTextStyle}\"", vendorView, StringComparison.Ordinal);
    }

    [Fact]
    public void Status_Heavy_Lists_Should_Use_Shared_Semantic_Status_Pills()
    {
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var orderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));

        Assert.Contains("Style=\"{StaticResource SemanticStatusPillStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Status, Converter={StaticResource StatusDisplayTextConverter}}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Tag=\"{Binding Status, StringFormat={}{0}}\"", purchaseOrderView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource SemanticStatusPillStyle}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Status, Converter={StaticResource StatusDisplayTextConverter}}\"", grnView, StringComparison.Ordinal);
        Assert.Contains("Tag=\"{Binding Status, StringFormat={}{0}}\"", grnView, StringComparison.Ordinal);
        Assert.DoesNotContain("DataGridTextColumn Header=\"Status\" Binding=\"{Binding Status}\"", grnView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource SemanticStatusPillStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Status, Converter={StaticResource StatusDisplayTextConverter}}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Tag=\"{Binding Status, StringFormat={}{0}}\"", quotationView, StringComparison.Ordinal);
        Assert.DoesNotContain("DataGridTextColumn Header=\"Status\" Binding=\"{Binding Status}\"", quotationView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource SemanticStatusPillStyle}\"", orderView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Status, Converter={StaticResource StatusDisplayTextConverter}}\"", orderView, StringComparison.Ordinal);
        Assert.Contains("Tag=\"{Binding Status, StringFormat={}{0}}\"", orderView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource SemanticStatusPillStyle}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Status, Converter={StaticResource StatusDisplayTextConverter}}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("Tag=\"{Binding Status, StringFormat={}{0}}\"", reportsView, StringComparison.Ordinal);
    }

    [Fact]
    public void Semantic_Status_Pills_Should_Use_Shared_Status_Tag_And_Display_Text_Contract()
    {
        var filesWithSemanticStatusPills = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(SolutionRoot, "Modules"), "*.xaml", SearchOption.AllDirectories))
        {
            var xaml = File.ReadAllText(file);
            if (!xaml.Contains("SemanticStatusPillStyle", StringComparison.Ordinal))
            {
                continue;
            }

            filesWithSemanticStatusPills.Add(Path.GetFileName(file));
            Assert.Contains("Tag=\"{Binding Status, StringFormat={}{0}}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding Status, Converter={StaticResource StatusDisplayTextConverter}}\"", xaml, StringComparison.Ordinal);
        }

        Assert.Equal(6, filesWithSemanticStatusPills.Count);
        Assert.Contains("PurchaseOrderView.xaml", filesWithSemanticStatusPills);
        Assert.Contains("GRNManagementView.xaml", filesWithSemanticStatusPills);
        Assert.Contains("QuotationManagementView.xaml", filesWithSemanticStatusPills);
        Assert.Contains("OrderManagementView.xaml", filesWithSemanticStatusPills);
        Assert.Contains("InventoryManagementView.xaml", filesWithSemanticStatusPills);
        Assert.Contains("ReportsView.xaml", filesWithSemanticStatusPills);
    }

    [Fact]
    public void Empty_State_Overlay_Copy_Should_Use_No_Title_And_Sentence_Description_Voice()
    {
        var overlayCount = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(SolutionRoot, "Modules"), "*.xaml", SearchOption.AllDirectories))
        {
            var xaml = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(
                         xaml,
                         "<controls:EmptyStateOverlay[\\s\\S]*?Title=\"([^\"]+)\"[\\s\\S]*?Description=\"([^\"]+)\"[\\s\\S]*?/>",
                         RegexOptions.Singleline))
            {
                overlayCount++;
                var title = match.Groups[1].Value;
                var description = match.Groups[2].Value;

                Assert.StartsWith("No ", title, StringComparison.Ordinal);
                Assert.EndsWith(".", description, StringComparison.Ordinal);
            }
        }

        Assert.Equal(5, overlayCount);
    }

    [Fact]
    public void Debounced_Management_Search_Rows_Should_Not_Render_Redundant_Search_Buttons()
    {
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
        var ironingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"));
        var orderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var paymentView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var salaryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var salesPurchaseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));

        foreach (var view in new[]
                 {
                     branchView, brandView, customerView, debtorView, expenseView, ironingView,
                     orderView, paymentView, salaryView, salesPurchaseView, vendorView
                 })
        {
            Assert.Contains("Style=\"{StaticResource FluentSearchTextBoxStyle}\"", view, StringComparison.Ordinal);
            Assert.DoesNotContain("Content=\"Search\"", view, StringComparison.Ordinal);
        }

        Assert.Contains("Style=\"{StaticResource FluentSearchTextBoxStyle}\"", categoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Search\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("h:DebouncedSearch.Command=\"{Binding SearchCategoriesCommand}\"", categoryView, StringComparison.Ordinal);
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

    [Fact]
    public void Master_Detail_Page_Shells_Should_Use_Shared_Grid_Style_And_Pane_Tokens()
    {
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var usersView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Users", "Views", "UserManagementView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var quotationView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var grnView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource MasterDetailPageGridStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailPaneGapWidth}\"", customerView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"16\"", customerView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource MasterDetailPageGridStyle}\"", usersView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailListPaneWidth}\"", usersView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailPaneGapWidth}\"", usersView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailEditorPaneWidth}\"", usersView, StringComparison.Ordinal);
        Assert.Contains("IsDefault=\"True\"", usersView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource MasterDetailPageGridStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailListPaneWidth}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailPaneGapWidth}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Width=\"{StaticResource MasterDetailEditorPaneWidth}\"", vendorView, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"12\"", vendorView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource MasterDetailPageGridStyle}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource MasterDetailPageGridStyle}\"", quotationView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource MasterDetailPageGridStyle}\"", grnView, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_State_Icons_And_Copy_Should_Use_Shared_Glyphs_And_Specific_No_Patterns()
    {
        var moduleFiles = Directory.GetFiles(
            Path.Combine(SolutionRoot, "Modules"),
            "*.xaml",
            SearchOption.AllDirectories);

        foreach (var file in moduleFiles)
        {
            var xaml = File.ReadAllText(file);

            foreach (Match match in Regex.Matches(xaml, "h:DataGridEmptyState\\.Icon=\"([^\"]+)\""))
            {
                var icon = match.Groups[1].Value;
                Assert.StartsWith("{StaticResource EmptyState", icon, StringComparison.Ordinal);
                Assert.DoesNotContain("&#x1F", icon, StringComparison.Ordinal);
                Assert.DoesNotContain("ðŸ", icon, StringComparison.Ordinal);
            }

            foreach (Match match in Regex.Matches(xaml, "h:DataGridEmptyState\\.Title=\"([^\"]+)\""))
            {
                var title = match.Groups[1].Value;
                Assert.StartsWith("No ", title, StringComparison.Ordinal);
                Assert.NotEqual("No entries found", title);
            }

            foreach (Match match in Regex.Matches(xaml, "h:DataGridEmptyState\\.Description=\"([^\"]+)\""))
            {
                var description = match.Groups[1].Value;
                Assert.EndsWith(".", description, StringComparison.Ordinal);
            }
        }

        var salesPurchaseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));
        Assert.Contains("h:DataGridEmptyState.Title=\"No sales and purchase entries found\"", salesPurchaseView, StringComparison.Ordinal);
    }

    [Fact]
    public void Module_Xaml_Should_Not_Contain_Corrupted_Currency_Or_Paginator_Glyph_Text()
    {
        var moduleFiles = Directory.GetFiles(
            Path.Combine(SolutionRoot, "Modules"),
            "*.xaml",
            SearchOption.AllDirectories);

        foreach (var file in moduleFiles)
        {
            var xaml = File.ReadAllText(file);
            Assert.DoesNotContain("â‚¹", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Content=\"â—€\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Content=\"â–¶\"", xaml, StringComparison.Ordinal);
        }

        var expenseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        Assert.Contains("Content=\"Prev\"", expenseView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Next\"", expenseView, StringComparison.Ordinal);
    }

    [Fact]
    public void Money_Entry_Forms_Should_Use_Currency_Adornments_Instead_Of_Label_Suffixes()
    {
        var branchView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"));
        var debtorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"));
        var paymentView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var orderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var ironingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"));
        var salaryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var inwardView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));
        var taxView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"));
        var orderViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "ViewModels", "OrderManagementViewModel.cs"));
        var ironingViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Ironing", "ViewModels", "IroningManagementViewModel.cs"));
        var salaryViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "ViewModels", "SalaryManagementViewModel.cs"));
        var inwardViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "ViewModels", "InwardEntryViewModel.cs"));
        var taxViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "ViewModels", "TaxManagementViewModel.cs"));

        Assert.DoesNotContain("Text=\"Amount ₹\"", branchView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Amount\"", branchView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", branchView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", branchView, StringComparison.Ordinal);

        Assert.DoesNotContain("Text=\"Total ₹\"", debtorView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Paid ₹\"", debtorView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Total amount\"", debtorView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Paid amount\"", debtorView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", debtorView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", debtorView, StringComparison.Ordinal);

        Assert.DoesNotContain("Text=\"Amount ₹\"", paymentView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Amount\"", paymentView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", paymentView, StringComparison.Ordinal);
        Assert.Contains("h:NumericInput.IsDecimalOnly=\"True\"", paymentView, StringComparison.Ordinal);

        Assert.DoesNotContain("Text=\"Rate ₹\"", orderView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Rate per item\"", orderView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", orderView, StringComparison.Ordinal);
        Assert.Contains("public string CurrencySymbol => regional.CurrencySymbol;", orderViewModel, StringComparison.Ordinal);

        Assert.DoesNotContain("Text=\"Rate ₹\"", ironingView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Rate per item\"", ironingView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", ironingView, StringComparison.Ordinal);
        Assert.Contains("public string CurrencySymbol => regional.CurrencySymbol;", ironingViewModel, StringComparison.Ordinal);

        Assert.DoesNotContain("Text=\"Base Salary ₹\"", salaryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Advance ₹\"", salaryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Incentive ₹\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Base salary\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Advance\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Incentive\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", salaryView, StringComparison.Ordinal);
        Assert.Contains("public string CurrencySymbol => regional.CurrencySymbol;", salaryViewModel, StringComparison.Ordinal);

        Assert.DoesNotContain("Text=\"Transport Charges (₹)\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Transport charges\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Transport charges\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("public string CurrencySymbol => regional.CurrencySymbol;", inwardViewModel, StringComparison.Ordinal);

        Assert.DoesNotContain("Text=\"Price From ₹\"", taxView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Price To ₹\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Price from\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Price to\"", taxView, StringComparison.Ordinal);
        Assert.Contains("h:TextBoxAdornment.PrefixText=\"{Binding CurrencySymbol}\"", taxView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Minimum amount\"", taxView, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"Maximum amount (optional)\"", taxView, StringComparison.Ordinal);
        Assert.Contains("public string CurrencySymbol => regional.CurrencySymbol;", taxViewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Module_Xaml_Should_Block_Known_Standardization_Drift_Patterns()
    {
        var moduleFiles = Directory.GetFiles(
            Path.Combine(SolutionRoot, "Modules"),
            "*.xaml",
            SearchOption.AllDirectories);

        foreach (var file in moduleFiles)
        {
            var xaml = File.ReadAllText(file);

            Assert.DoesNotContain("Style=\"{StaticResource ToolbarButtonStyle}\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("HasErrorMessage", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("HasSuccessMessage", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("Width=\"260\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("MinWidth=\"300\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("MinWidth=\"360\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("MinWidth=\"560\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("MaxWidth=\"720\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("MaxHeight=\"400\"", xaml, StringComparison.Ordinal);

            Assert.DoesNotMatch(
                new Regex("<TextBlock[^>]*PageTitleStyle.*?Management", RegexOptions.Singleline),
                xaml);
            Assert.DoesNotMatch(
                new Regex("<Run Text=\" [^\"]*Management\"", RegexOptions.Singleline),
                xaml);
            Assert.DoesNotMatch(
                new Regex("Header=\"[^\"]*Management\"", RegexOptions.Singleline),
                xaml);
        }
    }

    [Fact]
    public void Back_Navigation_Detail_Pages_Should_Use_Shared_Header_And_Subtitle_Contract()
    {
        var inwardView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));
        var variantView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource BackButtonStyle}\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageTitleStyle}\"", inwardView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", inwardView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource BackButtonStyle}\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageTitleStyle}\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageSubtitleStyle}\"", variantView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Manage product variants, adjust prices and stock, and create combinations in bulk.\"", variantView, StringComparison.Ordinal);
    }

    [Fact]
    public void Master_Data_Editors_Should_Use_Shared_Operational_Editor_Sections()
    {
        var brandView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"));
        var categoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"));
        var usersView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Users", "Views", "UserManagementView.xaml"));
        var taxView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionCardStyle}\"", brandView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", brandView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", brandView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FormActionRowStyle}\"", brandView, StringComparison.Ordinal);

        Assert.True(
            Regex.Matches(categoryView, "Style=\\\"\\{StaticResource OperationalEditorSectionCardStyle\\}\\\"").Count >= 2,
            "Category management should use shared operational editor cards for both type and category editors.");
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", categoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FormActionRowStyle}\"", categoryView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionCardStyle}\"", usersView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", usersView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", usersView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FormActionRowStyle}\"", usersView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Change PIN\" Style=\"{StaticResource SectionHeaderStyle}\"", usersView, StringComparison.Ordinal);

        Assert.True(
            Regex.Matches(taxView, "Style=\\\"\\{StaticResource OperationalEditorSectionCardStyle\\}\\\"").Count >= 4,
            "Tax management should use shared operational editor cards for rates, groups, slabs, and HSN sections.");
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FormActionRowStyle}\"", taxView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Price-Based Slabs\" Style=\"{StaticResource SectionHeaderStyle}\"", taxView, StringComparison.Ordinal);
    }

    [Fact]
    public void Long_Form_Editors_Should_Use_Shared_Sticky_Footer_Action_Bars()
    {
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource StickyFooterActionBarStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DirtyStateSummaryText}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DirtyStateHintTextStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding IsDirty}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("IsDefault=\"True\"", settingsView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource StickyFooterActionBarStyle}\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DirtyStateSummaryText}\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DirtyStateHintTextStyle}\"", firmView, StringComparison.Ordinal);
        Assert.True(
            firmView.Contains("IsEnabled=\"{Binding IsDirty}\"", StringComparison.Ordinal)
            || firmView.Contains("IsEnabled=\"{Binding CanSaveFirm}\"", StringComparison.Ordinal),
            "Firm settings should bind sticky-footer save availability through the view model.");
        Assert.Contains("IsDefault=\"True\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("MinWidth=\"120\"", firmView, StringComparison.Ordinal);
        Assert.True(
            Regex.Matches(firmView, "Style=\\\"\\{StaticResource OperationalEditorSectionCardStyle\\}\\\"").Count >= 4,
            "Firm settings should use shared operational editor cards for the business profile, compliance, billing, and receipt sections.");
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Business Profile\" Style=\"{StaticResource SectionHeaderStyle}\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Tax &amp; Compliance\" Style=\"{StaticResource SectionHeaderStyle}\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Billing &amp; Regional Rules\" Style=\"{StaticResource SectionHeaderStyle}\"", firmView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Invoice &amp; Receipt\" Style=\"{StaticResource SectionHeaderStyle}\"", firmView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource StickyFooterActionBarStyle}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DirtyStateSummaryText}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DirtyStateHintTextStyle}\"", productView, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding IsDirty}\"", productView, StringComparison.Ordinal);
        Assert.Contains("IsDefault=\"True\"", productView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ViewButtonStyle}\"", productView, StringComparison.Ordinal);
    }

    [Fact]
    public void Long_Form_Editors_Should_Use_Shared_Validation_Summary_Panels()
    {
        var settingsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource ValidationSummaryCardStyle}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding ValidationErrors}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding HasValidationErrors, Converter={StaticResource BoolToVisibility}}\"", settingsView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource ValidationSummaryCardStyle}\"", firmView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding ValidationErrors}\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding HasValidationErrors, Converter={StaticResource BoolToVisibility}}\"", firmView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource ValidationSummaryCardStyle}\"", productView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding ValidationErrors}\"", productView, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding HasValidationErrors, Converter={StaticResource BoolToVisibility}}\"", productView, StringComparison.Ordinal);
    }

    [Fact]
    public void Transaction_Entry_Editors_Should_Use_Shared_Operational_Editor_Cards()
    {
        var branchView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"));
        var debtorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"));
        var expenseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        var ironingView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"));
        var orderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var paymentView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var salaryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var salesPurchaseView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));

        foreach (var xaml in new[]
                 {
                     branchView, debtorView, expenseView, ironingView,
                     orderView, paymentView, salaryView, salesPurchaseView
                 })
        {
            Assert.Contains("Style=\"{StaticResource OperationalEditorSectionCardStyle}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Style=\"{StaticResource FormActionRowStyle}\"", xaml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Text=\"Add branch bill\" Style=\"{StaticResource SectionHeaderStyle}\"", branchView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Add debtor\" Style=\"{StaticResource SectionHeaderStyle}\"", debtorView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Add expense\" Style=\"{StaticResource SectionHeaderStyle}\"", expenseView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Add ironing entry\" Style=\"{StaticResource SectionHeaderStyle}\"", ironingView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Add order\" Style=\"{StaticResource SectionHeaderStyle}\"", orderView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Record payment\" Style=\"{StaticResource SectionHeaderStyle}\"", paymentView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Add salary record\" Style=\"{StaticResource SectionHeaderStyle}\"", salaryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Add entry\" Style=\"{StaticResource SectionHeaderStyle}\"", salesPurchaseView, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_Operational_Surfaces_Should_Use_Shared_Section_Card_Framing()
    {
        var inventoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"));

        Assert.True(
            Regex.Matches(inventoryView, "Style=\\\"\\{StaticResource OperationalEditorSectionCardStyle\\}\\\"").Count >= 4,
            "Inventory should use shared operational section cards for adjustment, alert, and stock take action surfaces.");
        Assert.Contains("Text=\"Stock Adjustment Details\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Stock Movement History\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Recent Stock Takes\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Save counted quantities as you work, then complete or cancel the active stock take from the same section.\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionTitleStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource OperationalEditorSectionDescriptionStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionCardStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionTitleStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionDescriptionStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Recent Stock Takes\" Style=\"{StaticResource SectionHeaderStyle}\"", inventoryView, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Only_Inspection_Surfaces_Should_Use_Shared_Inspection_Cards_And_Height_Tokens()
    {
        var customerView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var inventoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"));
        var cashRegisterView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "CashRegisterView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));

        Assert.Contains("MaxHeight=\"{StaticResource SubordinateDetailRegionMaxHeight}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionCardStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionTitleStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionDescriptionStyle}\"", customerView, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{StaticResource ReadOnlyInspectionGridMaxHeight}\"", customerView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"320\"", customerView, StringComparison.Ordinal);

        Assert.Contains("Text=\"Stock Movement History\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Recent Stock Takes\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionCardStyle}\"", inventoryView, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{StaticResource ReadOnlyInspectionGridMaxHeight}\"", inventoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"200\"", inventoryView, StringComparison.Ordinal);

        Assert.Contains("Text=\"Today's Movements\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Register History\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionCardStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionTitleStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionDescriptionStyle}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{StaticResource ReadOnlyInspectionGridMaxHeight}\"", cashRegisterView, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"200\"", cashRegisterView, StringComparison.Ordinal);

        Assert.Contains("Text=\"Ledger Entries\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionCardStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionTitleStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyInspectionSectionDescriptionStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{StaticResource ReadOnlyInspectionGridMaxHeight}\"", vendorView, StringComparison.Ordinal);
    }

    [Fact]
    public void Financial_Views_Should_Use_Culture_Aware_Currency_Formatting()
    {
        foreach (var xamlFile in Directory.GetFiles(
                     Path.Combine(SolutionRoot, "Modules"),
                     "*.xaml",
                     SearchOption.AllDirectories))
        {
            var xaml = File.ReadAllText(xamlFile);
            Assert.DoesNotContain("₹", xaml, StringComparison.Ordinal);
        }

        var branchView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"));
        var paymentsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));
        var productView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var taxView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"));

        Assert.Contains("StringFormat={}{0:C0}", branchView, StringComparison.Ordinal);
        Assert.Contains("StringFormat={}{0:C0}", paymentsView, StringComparison.Ordinal);
        Assert.Contains("StringFormat={}{0:C0}", reportsView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Sale\"", productView, StringComparison.Ordinal);
        Assert.Contains("Header=\"Cost\"", productView, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Sale ₹\"", productView, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"Cost ₹\"", productView, StringComparison.Ordinal);
        Assert.Contains("Header=\"From\"", taxView, StringComparison.Ordinal);
        Assert.Contains("Header=\"To\"", taxView, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"From ₹\"", taxView, StringComparison.Ordinal);
        Assert.DoesNotContain("Header=\"To ₹\"", taxView, StringComparison.Ordinal);
    }

    [Fact]
    public void Detail_Overlays_And_Read_Only_Previews_Should_Use_Shared_Surface_Styles()
    {
        var saleHistoryView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"));
        var vendorView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource ReadOnlyPreviewSurfaceStyle}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyPreviewScrollViewerStyle}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ReadOnlyPreviewTextStyle}\"", saleHistoryView, StringComparison.Ordinal);
        Assert.Contains("Text=\"Receipt Preview\"", saleHistoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Auto\"", saleHistoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("PanningMode=\"Both\"", saleHistoryView, StringComparison.Ordinal);
        Assert.DoesNotContain("TextWrapping=\"NoWrap\"", saleHistoryView, StringComparison.Ordinal);

        Assert.Contains("Style=\"{StaticResource DetailOverlayHostStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DetailOverlayHeaderRowStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DetailOverlayTitleStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DetailOverlaySubtitleStyle}\"", vendorView, StringComparison.Ordinal);
        Assert.Contains("Supplier Ledger", vendorView, StringComparison.Ordinal);
        Assert.Contains("selected vendor context", vendorView, StringComparison.Ordinal);
    }

    [Fact]
    public void Destructive_Admin_Flows_Should_Use_Shared_Dialog_Confirmation_And_Static_Warning_Callouts()
    {
        var backupViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "ViewModels", "BackupRestoreViewModel.cs"));
        var backupView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Backup", "Views", "BackupRestoreView.xaml"));
        var financialYearViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "FinancialYears", "ViewModels", "FinancialYearViewModel.cs"));
        var financialYearView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "FinancialYears", "Views", "FinancialYearView.xaml"));
        var settingsViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "ViewModels", "SystemSettingsViewModel.cs"));

        Assert.Contains("dialogService.Confirm(", backupViewModel, StringComparison.Ordinal);
        Assert.Contains("\"Restore Backup\"", backupViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("IsRestoreConfirmVisible", backupViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelRestore", backupViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfirmRestoreCommand", backupView, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelRestoreCommand", backupView, StringComparison.Ordinal);
        Assert.DoesNotContain("IsRestoreConfirmVisible", backupView, StringComparison.Ordinal);
        Assert.Contains("shared restore dialog", backupView, StringComparison.Ordinal);

        Assert.Contains("dialogService.Confirm(", financialYearViewModel, StringComparison.Ordinal);
        Assert.Contains("\"Reset Billing\"", financialYearViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("IsConfirmingReset", financialYearViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelReset", financialYearViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfirmResetCommand", financialYearView, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelResetCommand", financialYearView, StringComparison.Ordinal);
        Assert.DoesNotContain("IsConfirmingReset", financialYearView, StringComparison.Ordinal);
        Assert.Contains("The shared confirmation dialog", financialYearView, StringComparison.Ordinal);

        Assert.Contains("\"Restore Database\"", settingsViewModel, StringComparison.Ordinal);
        Assert.Contains("\"Factory Reset\"", settingsViewModel, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(settingsViewModel, "dialogService\\.Confirm\\(").Count);
    }

    [Fact]
    public void Ui_Standardization_Charter_Should_Define_Product_Level_Governance_Rules()
    {
        var charter = File.ReadAllText(
            Path.Combine(SolutionRoot, "docs", "ui", "UI_STANDARDIZATION_CHARTER.md"));

        Assert.Contains("# UI Standardization Charter", charter, StringComparison.Ordinal);
        Assert.Contains("## Source Of Truth", charter, StringComparison.Ordinal);
        Assert.Contains("## Universal Action Rules", charter, StringComparison.Ordinal);
        Assert.Contains("## Page Classes", charter, StringComparison.Ordinal);
        Assert.Contains("## State Surfaces", charter, StringComparison.Ordinal);
        Assert.Contains("## Input Rules", charter, StringComparison.Ordinal);
        Assert.Contains("## Dialog Taxonomy", charter, StringComparison.Ordinal);
        Assert.Contains("## Edit Surface Decision Rules", charter, StringComparison.Ordinal);
        Assert.Contains("## Disclosure And Overflow Rules", charter, StringComparison.Ordinal);
        Assert.Contains("## Reporting Rules", charter, StringComparison.Ordinal);
        Assert.Contains("## Responsive And Hover Rules", charter, StringComparison.Ordinal);
        Assert.Contains("## Governance", charter, StringComparison.Ordinal);
        Assert.Contains("UiStandardizationStandardsTests", charter, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_Loading_Overlays_Should_Use_Shared_Working_State_Contract()
    {
        var baseViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Base", "BaseViewModel.cs"));
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));
        var loginViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "ViewModels", "LoginViewModel.cs"));
        var firmView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "Views", "FirmManagementView.xaml"));
        var firmViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Firm", "ViewModels", "FirmViewModel.cs"));

        Assert.Contains("public bool IsWorking => IsBusy || IsLoading;", baseViewModel, StringComparison.Ordinal);
        Assert.Contains("public virtual string WorkingMessage => IsLoading", baseViewModel, StringComparison.Ordinal);
        Assert.Contains("partial void OnIsBusyChanged(bool value)", baseViewModel, StringComparison.Ordinal);
        Assert.Contains("partial void OnIsLoadingChanged(bool value)", baseViewModel, StringComparison.Ordinal);
        Assert.Contains("OnPropertyChanged(nameof(IsWorking));", baseViewModel, StringComparison.Ordinal);
        Assert.Contains("OnPropertyChanged(nameof(WorkingMessage));", baseViewModel, StringComparison.Ordinal);

        Assert.DoesNotContain("<controls:LoadingOverlay IsActive=\"{Binding IsWorking}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("<controls:ProgressRing IsActive=\"{Binding IsWorking}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding WorkingMessage}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Please wait a moment.", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("<Condition Binding=\"{Binding IsForgotPinMode}\" Value=\"True\"/>", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("<Condition Binding=\"{Binding IsWorking}\" Value=\"True\"/>", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("IsActive=\"{Binding IsVerifying}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Message=\"Verifying login...\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("Verifying login...", loginViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("Resetting PIN...", loginViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("public override string WorkingMessage", loginViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("partial void OnIsForgotPinModeChanged(bool value) => OnPropertyChanged(nameof(WorkingMessage));", loginViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("public partial bool IsVerifying { get; set; }", loginViewModel, StringComparison.Ordinal);

        Assert.Contains("LoadingOverlay IsActive=\"{Binding IsWorking}\"", firmView, StringComparison.Ordinal);
        Assert.Contains("Message=\"{Binding WorkingMessage}\"", firmView, StringComparison.Ordinal);
        Assert.Contains("public override string WorkingMessage => IsLoading", firmViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("public bool IsWorking => IsBusy || IsLoading;", firmViewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_Action_Commands_Should_Use_Shared_Link_Button_Styles()
    {
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var loginView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));

        Assert.Contains("<Style x:Key=\"ToolbarLinkButtonStyle\" TargetType=\"Button\">", posStyles, StringComparison.Ordinal);
        Assert.Contains("Content=\"Forgot PIN?\"", loginView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ToolbarLinkButtonStyle}\"", loginView, StringComparison.Ordinal);
        Assert.DoesNotContain("<Hyperlink Command=\"{Binding ForgotPinCommand}\">", loginView, StringComparison.Ordinal);
    }

    [Fact]
    public void Inline_Edit_Hint_Icons_Should_Use_Shared_Hover_Reveal_Style()
    {
        var brandsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"));
        var categoriesView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"));

        Assert.Contains("Style=\"{StaticResource DataGridInlineEditHintIconStyle}\"", brandsView, StringComparison.Ordinal);
        Assert.DoesNotContain("Path=IsMouseOver", brandsView, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"PencilIcon\"", brandsView, StringComparison.Ordinal);

        Assert.Equal(2, Regex.Matches(categoriesView, "Style=\\\"\\{StaticResource DataGridInlineEditHintIconStyle\\}\\\"").Count);
        Assert.DoesNotContain("Path=IsMouseOver", categoriesView, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"PencilIcon\"", categoriesView, StringComparison.Ordinal);
    }

    private static string FindXaml(string fileName)
    {
        var matches = Directory.GetFiles(SolutionRoot, fileName, SearchOption.AllDirectories);
        return Assert.Single(matches);
    }

    private static int CountFilesContaining(string token)
    {
        return Directory.GetFiles(Path.Combine(SolutionRoot, "Modules"), "*.xaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(SolutionRoot, "Core"), "*.xaml", SearchOption.AllDirectories))
            .Count(file => File.ReadAllText(file).Contains(token, StringComparison.Ordinal));
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
