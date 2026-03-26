namespace StoreAssistantPro.Models.Commercial;

/// <summary>License tiers (#543-545).</summary>
public enum LicenseTier { Free, Basic, Pro, Enterprise }

/// <summary>License record (#541-548).</summary>
public sealed class LicenseInfo
{
    public int Id { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public LicenseTier Tier { get; set; } = LicenseTier.Free;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsTrial { get; set; }
    public string? MachineId { get; set; }
    public int MaxUsers { get; set; } = 1;
    public int MaxProducts { get; set; } = 50;
    public bool IsActive { get; set; } = true;
}

/// <summary>White-label branding (#559-567).</summary>
public sealed class WhiteLabelConfig
{
    public string AppName { get; set; } = "StoreAssistantPro";
    public string? LogoPath { get; set; }
    public string? SplashImagePath { get; set; }
    public string? PrimaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? SupportEmail { get; set; }
    public string? OnboardingText { get; set; }
}

/// <summary>Usage analytics event (#549).</summary>
public sealed record UsageEvent(
    string EventName,
    string? Category,
    DateTime OccurredAt,
    Dictionary<string, string>? Properties);

/// <summary>Plan comparison entry (#555).</summary>
public sealed record PlanFeatureComparison(
    string FeatureName,
    bool FreeIncluded,
    bool BasicIncluded,
    bool ProIncluded,
    bool EnterpriseIncluded);
