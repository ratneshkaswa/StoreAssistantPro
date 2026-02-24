using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published by <see cref="Services.IOnboardingJourneyService"/> when
/// the operator's <see cref="UserExperienceLevel"/> changes for any
/// reason — automatic promotion based on usage milestones, manual
/// override via admin settings, or onboarding reset.
///
/// <para>
/// Subscribers (tip rotation, help-context, settings UI) use this to
/// immediately adapt guidance depth, tip visibility, and onboarding
/// indicators without polling.
/// </para>
///
/// <para><b>Common <see cref="PromotionReason"/> values:</b></para>
/// <list type="bullet">
///   <item><c>"Opened N distinct windows"</c> — auto-promotion rule.</item>
///   <item><c>"Completed N sessions"</c> — auto-promotion rule.</item>
///   <item><c>"Completed N billing sessions"</c> — auto-promotion rule.</item>
///   <item><c>"Manual override"</c> — admin changed the level.</item>
///   <item><c>"Reset"</c> — onboarding journey was reset.</item>
/// </list>
/// </summary>
public sealed record ExperienceLevelPromotedEvent(
    UserExperienceLevel PreviousLevel,
    UserExperienceLevel NewLevel,
    string PromotionReason) : IEvent;
