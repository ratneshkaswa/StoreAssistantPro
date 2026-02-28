using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton service that registers all first-time onboarding tips
/// into <see cref="ITipRegistryService"/> at startup and auto-dismisses
/// them when the operator graduates past
/// <see cref="UserExperienceLevel.Beginner"/>.
///
/// <para><b>Startup behavior:</b></para>
/// <list type="number">
///   <item>Calls <see cref="OnboardingTipDefinitions.CreateAll"/> to
///         get the full catalog of onboarding tips.</item>
///   <item>Registers each tip via
///         <see cref="ITipRegistryService.Register(IEnumerable{TipDefinition})"/>.</item>
///   <item>Subscribes to <see cref="ExperienceLevelPromotedEvent"/>
///         on the <see cref="IEventBus"/>.</item>
/// </list>
///
/// <para><b>Auto-dismiss on promotion:</b></para>
/// <para>
/// When <see cref="ExperienceLevelPromotedEvent"/> fires and the
/// <see cref="ExperienceLevelPromotedEvent.PreviousLevel"/> was
/// <see cref="UserExperienceLevel.Beginner"/>, the registrar
/// iterates <see cref="OnboardingTipDefinitions.AllTipIds"/> and
/// calls <see cref="ITipStateService.DismissTip"/> for each one
/// that hasn't already been dismissed.
/// </para>
/// <para>
/// This ensures onboarding tips disappear silently when the operator
/// reaches Intermediate — no manual close required. Tips that were
/// already dismissed by the user are unaffected (idempotent).
/// </para>
///
/// <para><b>Reset support:</b></para>
/// <para>
/// When the event's <see cref="ExperienceLevelPromotedEvent.NewLevel"/>
/// is <see cref="UserExperienceLevel.Beginner"/> (i.e. a journey
/// reset), the registrar calls <see cref="ITipStateService.ResetTip"/>
/// for each onboarding tip so they reappear for the fresh beginner
/// phase.
/// </para>
///
/// <para><b>Lifetime:</b> Registered as a <b>singleton</b>.
/// Implements <see cref="IDisposable"/> to unsubscribe from the
/// event bus at shutdown.</para>
/// </summary>
public sealed class OnboardingTipRegistrar : IDisposable
{
    private readonly ITipRegistryService _registry;
    private readonly ITipStateService _tipState;
    private readonly ITipRotationService _tipRotation;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OnboardingTipRegistrar> _logger;

    public OnboardingTipRegistrar(
        ITipRegistryService registry,
        ITipStateService tipState,
        ITipRotationService tipRotation,
        IEventBus eventBus,
        ILogger<OnboardingTipRegistrar> logger)
    {
        _registry = registry;
        _tipState = tipState;
        _tipRotation = tipRotation;
        _eventBus = eventBus;
        _logger = logger;

        // Register all onboarding tips into the tip registry.
        var tips = OnboardingTipDefinitions.CreateAll();
        _registry.Register(tips);
        _logger.LogInformation(
            "Registered {Count} onboarding tips", tips.Count);

        // React to experience-level changes.
        _eventBus.Subscribe<ExperienceLevelPromotedEvent>(OnExperienceLevelPromotedAsync);
    }

    // ── Event handler ──────────────────────────────────────────────

    private Task OnExperienceLevelPromotedAsync(ExperienceLevelPromotedEvent evt)
    {
        // Operator graduated past Beginner → auto-dismiss all
        // remaining onboarding tips so they disappear silently.
        if (evt.PreviousLevel == UserExperienceLevel.Beginner
            && evt.NewLevel > UserExperienceLevel.Beginner)
        {
            AutoDismissAll();
        }

        // Journey was reset back to Beginner → restore onboarding
        // tips so the operator sees them again.
        if (evt.NewLevel == UserExperienceLevel.Beginner
            && evt.PreviousLevel > UserExperienceLevel.Beginner)
        {
            RestoreAll();
        }

        return Task.CompletedTask;
    }

    // ── Bulk operations ────────────────────────────────────────────

    private void AutoDismissAll()
    {
        var dismissed = 0;
        foreach (var tipId in OnboardingTipDefinitions.AllTipIds)
        {
            if (!_tipState.IsTipDismissed(tipId))
            {
                _tipState.DismissTip(tipId);
                dismissed++;
            }
        }

        if (dismissed > 0)
        {
            _logger.LogInformation(
                "Auto-dismissed {Count} onboarding tip(s) on promotion past Beginner",
                dismissed);
            _tipRotation.InvalidateAll();
        }
    }

    private void RestoreAll()
    {
        var restored = 0;
        foreach (var tipId in OnboardingTipDefinitions.AllTipIds)
        {
            if (_tipState.IsTipDismissed(tipId))
            {
                _tipState.ResetTip(tipId);
                restored++;
            }
        }

        if (restored > 0)
        {
            _logger.LogInformation(
                "Restored {Count} onboarding tip(s) on journey reset to Beginner",
                restored);
            _tipRotation.InvalidateAll();
        }
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _eventBus.Unsubscribe<ExperienceLevelPromotedEvent>(OnExperienceLevelPromotedAsync);
    }
}
