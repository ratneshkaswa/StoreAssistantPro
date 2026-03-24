using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class NumericInputScopeStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void NumericInput_Should_Expose_InputScope_Hints_For_Touch_Optimized_Fields()
    {
        var helper = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "NumericInput.cs"));

        Assert.Contains("Scope\", typeof(InputScopeNameValue)", helper, StringComparison.Ordinal);
        Assert.Contains("InputScopeNameValue.Digits", helper, StringComparison.Ordinal);
        Assert.Contains("InputScopeNameValue.Number", helper, StringComparison.Ordinal);
        Assert.Contains("textBox.InputScope =", helper, StringComparison.Ordinal);
    }

    [Fact]
    public void NumberBox_Template_Should_Default_To_Number_InputScope()
    {
        var theme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("x:Name=\"PART_TextBox\"", theme, StringComparison.Ordinal);
        Assert.Contains("InputScope=\"Number\"", theme, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Modules\\Firm\\Views\\FirmManagementView.xaml", "h:NumericInput.Scope=\"TelephoneNumber\"")]
    [InlineData("Modules\\Firm\\Views\\FirmManagementView.xaml", "h:NumericInput.Scope=\"Digits\"")]
    [InlineData("Modules\\Vendors\\Views\\VendorManagementView.xaml", "h:NumericInput.Scope=\"TelephoneNumber\"")]
    [InlineData("Modules\\Vendors\\Views\\VendorManagementView.xaml", "h:NumericInput.Scope=\"Digits\"")]
    [InlineData("Modules\\Customers\\Views\\CustomerManagementView.xaml", "h:NumericInput.Scope=\"TelephoneNumber\"")]
    [InlineData("Modules\\Payments\\Views\\PaymentManagementView.xaml", "h:NumericInput.Scope=\"Number\"")]
    [InlineData("Modules\\Products\\Views\\ProductManagementView.xaml", "h:NumericInput.Scope=\"Number\"")]
    [InlineData("Modules\\Debtors\\Views\\DebtorManagementView.xaml", "h:NumericInput.Scope=\"Number\"")]
    public void HighTraffic_Forms_Should_Use_Touch_Friendly_InputScope_Hints(string relativePath, string expectedMarkup)
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains(expectedMarkup, content, StringComparison.Ordinal);
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
