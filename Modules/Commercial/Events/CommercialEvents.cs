using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Commercial;

namespace StoreAssistantPro.Modules.Commercial.Events;

/// <summary>Published when a license is activated or updated.</summary>
public sealed class LicenseActivatedEvent(LicenseInfo license) : IEvent
{
    public LicenseInfo License { get; } = license;
}

/// <summary>Published when a subscription plan changes.</summary>
public sealed class PlanChangedEvent(LicenseTier oldTier, LicenseTier newTier) : IEvent
{
    public LicenseTier OldTier { get; } = oldTier;
    public LicenseTier NewTier { get; } = newTier;
}
