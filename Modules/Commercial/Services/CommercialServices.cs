using Microsoft.Extensions.Logging;
using StoreAssistantPro.Models.Commercial;

namespace StoreAssistantPro.Modules.Commercial.Services;

public sealed class LicenseService(ILogger<LicenseService> logger) : ILicenseService
{
    private LicenseInfo _current = new() { IsTrial = true, Tier = LicenseTier.Free, ActivatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) };

    public Task<LicenseInfo?> GetCurrentLicenseAsync(CancellationToken ct = default) => Task.FromResult<LicenseInfo?>(_current);

    public Task<bool> ValidateLicenseKeyAsync(string key, CancellationToken ct = default)
    {
        var valid = !string.IsNullOrWhiteSpace(key) && key.Length >= 16;
        logger.LogInformation("License key validation: {Valid}", valid);
        return Task.FromResult(valid);
    }

    public async Task<LicenseInfo> ActivateLicenseAsync(string key, CancellationToken ct = default)
    {
        if (!await ValidateLicenseKeyAsync(key, ct).ConfigureAwait(false))
            throw new InvalidOperationException("Invalid license key");

        _current = new LicenseInfo
        {
            LicenseKey = key, Tier = LicenseTier.Pro, IsActive = true, IsTrial = false,
            ActivatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddYears(1),
            MachineId = Environment.MachineName, MaxUsers = 10, MaxProducts = int.MaxValue
        };
        logger.LogInformation("License activated: {Tier}", _current.Tier);
        return _current;
    }

    public Task<bool> IsTrialActiveAsync(CancellationToken ct = default) =>
        Task.FromResult(_current.IsTrial && _current.ExpiresAt > DateTime.UtcNow);

    public Task<int> GetTrialDaysRemainingAsync(CancellationToken ct = default) =>
        Task.FromResult(_current.IsTrial ? Math.Max(0, (_current.ExpiresAt!.Value - DateTime.UtcNow).Days) : 0);

    public Task<bool> IsFeatureAvailableAsync(string featureName, CancellationToken ct = default) =>
        Task.FromResult(_current.Tier >= LicenseTier.Pro || _current.IsTrial);

    public Task TransferLicenseAsync(string newMachineId, CancellationToken ct = default)
    {
        _current.MachineId = newMachineId;
        logger.LogInformation("License transferred to {Machine}", newMachineId);
        return Task.CompletedTask;
    }

    public async Task<bool> RenewLicenseAsync(string newKey, CancellationToken ct = default)
    {
        if (!await ValidateLicenseKeyAsync(newKey, ct).ConfigureAwait(false)) return false;
        _current.LicenseKey = newKey;
        _current.ExpiresAt = DateTime.UtcNow.AddYears(1);
        logger.LogInformation("License renewed until {Expiry}", _current.ExpiresAt);
        return true;
    }
}

public sealed class SubscriptionService(ILicenseService licenseService, ILogger<SubscriptionService> logger) : ISubscriptionService
{
    public async Task<LicenseTier> GetCurrentTierAsync(CancellationToken ct = default)
    {
        var license = await licenseService.GetCurrentLicenseAsync(ct).ConfigureAwait(false);
        return license?.Tier ?? LicenseTier.Free;
    }

    public Task<IReadOnlyList<PlanFeatureComparison>> GetPlanComparisonAsync(CancellationToken ct = default)
    {
        IReadOnlyList<PlanFeatureComparison> plans =
        [
            new("Products (max)", false, true, true, true),
            new("Users (max)", false, true, true, true),
            new("Reports", false, false, true, true),
            new("Inventory Management", false, false, true, true),
            new("Multi-Store", false, false, false, true),
            new("API Access", false, false, false, true),
            new("AI Features", false, false, false, true),
        ];
        return Task.FromResult(plans);
    }

    public Task<bool> UpgradeAsync(LicenseTier newTier, string licenseKey, CancellationToken ct = default)
    {
        logger.LogInformation("Upgrade to {Tier} requested", newTier);
        return Task.FromResult(true);
    }

    public Task HandleDowngradeAsync(LicenseTier newTier, CancellationToken ct = default)
    {
        logger.LogInformation("Downgrade to {Tier} handled", newTier);
        return Task.CompletedTask;
    }

    public Task<bool> ConvertTrialToPaidAsync(string licenseKey, CancellationToken ct = default)
    {
        logger.LogInformation("Trial-to-paid conversion");
        return Task.FromResult(true);
    }
}

public sealed class WhiteLabelService(ILogger<WhiteLabelService> logger) : IWhiteLabelService
{
    private WhiteLabelConfig _config = new();

    public WhiteLabelConfig GetConfig() => _config;

    public Task SaveConfigAsync(WhiteLabelConfig config, CancellationToken ct = default)
    {
        _config = config;
        logger.LogInformation("White-label config saved: {AppName}", config.AppName);
        return Task.CompletedTask;
    }

    public Task ApplyBrandingAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Applied branding: {AppName}", _config.AppName);
        return Task.CompletedTask;
    }
}

public sealed class UsageAnalyticsService(ILogger<UsageAnalyticsService> logger) : IUsageAnalyticsService
{
    private readonly List<Models.Commercial.UsageEvent> _buffer = [];

    public Task TrackEventAsync(string eventName, string? category = null, Dictionary<string, string>? properties = null, CancellationToken ct = default)
    {
        _buffer.Add(new Models.Commercial.UsageEvent(eventName, category, DateTime.UtcNow, properties));
        return Task.CompletedTask;
    }

    public Task FlushAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Flushing {Count} usage events", _buffer.Count);
        _buffer.Clear();
        return Task.CompletedTask;
    }
}
