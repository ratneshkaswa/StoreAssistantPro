using StoreAssistantPro.Models.Commercial;

namespace StoreAssistantPro.Modules.Commercial.Services;

/// <summary>License management service (#541-548).</summary>
public interface ILicenseService
{
    Task<LicenseInfo?> GetCurrentLicenseAsync(CancellationToken ct = default);
    Task<bool> ValidateLicenseKeyAsync(string key, CancellationToken ct = default);
    Task<LicenseInfo> ActivateLicenseAsync(string key, CancellationToken ct = default);
    Task<bool> IsTrialActiveAsync(CancellationToken ct = default);
    Task<int> GetTrialDaysRemainingAsync(CancellationToken ct = default);
    Task<bool> IsFeatureAvailableAsync(string featureName, CancellationToken ct = default);
    Task TransferLicenseAsync(string newMachineId, CancellationToken ct = default);
    Task<bool> RenewLicenseAsync(string newKey, CancellationToken ct = default);
}

/// <summary>Subscription and tier management (#550-558).</summary>
public interface ISubscriptionService
{
    Task<LicenseTier> GetCurrentTierAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PlanFeatureComparison>> GetPlanComparisonAsync(CancellationToken ct = default);
    Task<bool> UpgradeAsync(LicenseTier newTier, string licenseKey, CancellationToken ct = default);
    Task HandleDowngradeAsync(LicenseTier newTier, CancellationToken ct = default);
    Task<bool> ConvertTrialToPaidAsync(string licenseKey, CancellationToken ct = default);
}

/// <summary>White-label branding service (#559-567).</summary>
public interface IWhiteLabelService
{
    WhiteLabelConfig GetConfig();
    Task SaveConfigAsync(WhiteLabelConfig config, CancellationToken ct = default);
    Task ApplyBrandingAsync(CancellationToken ct = default);
}

/// <summary>Anonymous usage analytics (#549).</summary>
public interface IUsageAnalyticsService
{
    Task TrackEventAsync(string eventName, string? category = null, Dictionary<string, string>? properties = null, CancellationToken ct = default);
    Task FlushAsync(CancellationToken ct = default);
}
