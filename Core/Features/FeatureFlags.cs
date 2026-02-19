namespace StoreAssistantPro.Core.Features;

/// <summary>
/// Well-known feature names used across the application.
/// Centralised here so ViewModels and modules reference constants
/// instead of magic strings.
/// </summary>
public static class FeatureFlags
{
    public const string Products = "Products";
    public const string Sales = "Sales";
    public const string Billing = "Billing";
    public const string SystemSettings = "SystemSettings";
    public const string Reports = "Reports";
    public const string AdvancedBilling = "AdvancedBilling";
}
