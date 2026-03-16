using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class UiMicrocopyStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Theory]
    [MemberData(nameof(LegacyMicrocopyCases))]
    public void RuntimeWindows_Should_Not_Reintroduce_Removed_DecorativeMicrocopy(
        string relativePath,
        string forbiddenText)
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.DoesNotContain(forbiddenText, content, StringComparison.Ordinal);
    }

    public static TheoryData<string, string> LegacyMicrocopyCases =>
        new()
        {
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "Update the firm identity, tax profile, and billing behavior used across invoices, reports, and the workspace."
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "These details appear on bills, tax documents, and the signed-in workspace."
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "Keep GST, PAN, and state details aligned so filings and invoices stay clean."
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "These preferences control how invoices render, round, and interpret tax."
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "Keep this off if you want billing to stop when inventory is exhausted."
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "Required fields are marked with *."
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "ChangeStatusText"
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "ChangeStatusHint"
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "BusinessSummarySubtitle"
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "TaxSummarySubtitle"
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "InvoiceSummarySubtitle"
            },
            {
                Path.Combine("Modules", "Firm", "Views", "FirmManagementView.xaml"),
                "OperationsSummarySubtitle"
            },
            {
                Path.Combine("Modules", "Firm", "ViewModels", "FirmViewModel.cs"),
                "All changes saved"
            },
            {
                Path.Combine("Modules", "Firm", "ViewModels", "FirmViewModel.cs"),
                "Unsaved changes"
            },
            {
                Path.Combine("Modules", "Settings", "Views", "SystemSettingsView.xaml"),
                "Manage backups, restore operations, and workstation defaults without changing firm-wide billing rules."
            },
            {
                Path.Combine("Modules", "Settings", "Views", "SystemSettingsView.xaml"),
                "Protect the local database and control where backup files are written."
            },
            {
                Path.Combine("Modules", "Settings", "Views", "SystemSettingsView.xaml"),
                "These defaults affect the current machine without changing shared business rules."
            },
            {
                Path.Combine("Modules", "Settings", "Views", "SystemSettingsView.xaml"),
                "Used by the current workstation for bill and receipt printing."
            },
            {
                Path.Combine("Modules", "Settings", "Views", "SystemSettingsView.xaml"),
                "When enabled, the backup time becomes required and the app will use the selected backup location."
            },
            {
                Path.Combine("Modules", "Settings", "Views", "SystemSettingsView.xaml"),
                "Leave blank to use the Windows default printer."
            },
            {
                Path.Combine("Modules", "MainShell", "Views", "WorkspaceView.xaml"),
                "A lighter, calmer workspace for daily retail operations."
            },
            {
                Path.Combine("Modules", "MainShell", "Views", "WorkspaceView.xaml"),
                "Use the command bar above for frequent tasks, or start with one of the core work areas below."
            },
            {
                Path.Combine("Modules", "MainShell", "Views", "WorkspaceView.xaml"),
                "Business profile and defaults"
            },
            {
                Path.Combine("Modules", "MainShell", "Views", "WorkspaceView.xaml"),
                "Access, PINs, and roles"
            },
            {
                Path.Combine("Modules", "MainShell", "Views", "WorkspaceView.xaml"),
                "Reload dashboard and current data"
            },
            {
                Path.Combine("Modules", "MainShell", "Views", "WorkspaceView.xaml"),
                "Tip: Press Ctrl+D anytime to return to Home."
            },
            {
                Path.Combine("Modules", "Authentication", "Views", "LoginView.xaml"),
                "Choose Admin or User to unlock PIN entry."
            },
            {
                Path.Combine("Modules", "Authentication", "Views", "LoginView.xaml"),
                "Or use keyboard 0–9"
            },
            {
                Path.Combine("Modules", "Authentication", "Views", "LoginView.xaml"),
                "Enter Master PIN to reset"
            },
            {
                Path.Combine("Modules", "Inward", "Views", "InwardEntryView.xaml"),
                "Select 1–10 parcels for this inward entry."
            },
            {
                Path.Combine("Modules", "Inward", "Views", "InwardEntryView.xaml"),
                "Total transport charges will be split equally across parcels."
            },
            {
                Path.Combine("Modules", "Products", "Views", "ProductManagementView.xaml"),
                "Tax Rate is for quick billing. GST Group + HSN enables enterprise price-based slab resolution."
            },
            {
                Path.Combine("Modules", "Products", "Views", "ProductManagementView.xaml"),
                "Only enabled attributes will appear during inward entry and billing."
            }
        };

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
