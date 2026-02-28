using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton service that maintains a live <see cref="HelpContext"/> and
/// resolves context-specific help text via an ordered rule pipeline.
/// <para>
/// <b>Event-driven updates:</b> Subscribes to
/// <see cref="OperationalModeChangedEvent"/>,
/// <see cref="OfflineModeChangedEvent"/>, and
/// <see cref="ExperienceLevelPromotedEvent"/>, and listens for
/// <see cref="IFocusLockService.PropertyChanged"/> to refresh
/// the context immediately when the application state shifts.
/// Each refresh publishes <see cref="HelpContextChangedEvent"/>
/// so downstream consumers (tooltip panels, help overlays) can react.
/// </para>
/// <para>
/// <b>Rule pipeline:</b> Rules are evaluated in registration order.
/// Each rule can return a <see cref="ContextHelpResult"/> (match) or
/// <c>null</c> (pass). A suffix-only rule (e.g. offline warning) is
/// merged into the final result produced by a later content rule.
/// </para>
/// <para>
/// <b>Experience-level adaptation:</b> After the rule pipeline
/// produces a result, the <see cref="ExperienceLevelAdapter"/> post-
/// processes it based on the operator’s
/// <see cref="UserExperienceLevel"/>. Beginner operators receive
/// detailed, step-by-step explanations; advanced operators receive
/// short, concise tips; intermediate operators see the default
/// rule output unchanged.
/// </para>
/// <para>
/// <b>Adding new rules:</b> Implement <see cref="IContextHelpRule"/>
/// and add the instance to the <see cref="Rules"/> array. No other
/// wiring is needed — the pipeline picks it up automatically.
/// </para>
/// </summary>
public sealed class ContextHelpService : IContextHelpService
{
    private readonly IAppStateService _appState;
    private readonly IFocusLockService _focusLock;
    private readonly IOnboardingJourneyService _onboarding;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ContextHelpService> _logger;
    private readonly Lock _lock = new();

    private static readonly ExperienceLevelAdapter Adapter = new();

    private HelpContext _currentContext;

    // ── Generation-stamped resolution cache ─────────────────────────
    //
    // Each entry stores the result computed for a given key under a
    // specific context generation.  When AppState changes the generation
    // counter advances and stale entries are lazily replaced on next
    // access — no bulk invalidation needed.

    private long _generation;

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    private readonly record struct CacheEntry(long Generation, ContextHelpResult? Result);

    // ── Rule pipeline (evaluated in order) ─────────────────────────

    private static readonly IContextHelpRule[] Rules =
    [
        new OfflineWarningRule(),
        new FocusLockedRule(),
        new BillingSessionRule(),
        new ModeSpecificRule(),
        new FallbackRule(),
    ];

    // ── Constructor ────────────────────────────────────────────────

    public ContextHelpService(
        IAppStateService appState,
        IFocusLockService focusLock,
        IOnboardingJourneyService onboarding,
        IEventBus eventBus,
        ILogger<ContextHelpService> logger)
    {
        _appState = appState;
        _focusLock = focusLock;
        _onboarding = onboarding;
        _eventBus = eventBus;
        _logger = logger;

        _currentContext = HelpContext.From(appState, focusLock, onboarding);

        _eventBus.Subscribe<OperationalModeChangedEvent>(OnModeChangedAsync);
        _eventBus.Subscribe<OfflineModeChangedEvent>(OnOfflineChangedAsync);
        _eventBus.Subscribe<ExperienceLevelPromotedEvent>(OnExperienceLevelChangedAsync);
        _focusLock.PropertyChanged += OnFocusLockPropertyChanged;

        // Register as the global resolver so SmartTooltip can call
        // back into this service at hover time without a DI reference.
        Helpers.SmartTooltip.ContextResolver = Resolve;

        // Register as the context resolver for InlineTipBanner so
        // TipBannerAutoState can adapt tip text to the current mode.
        Helpers.TipBannerAutoState.ContextResolver = Resolve;
        Helpers.TipBannerAutoState.ContextChangedCallback =
            Helpers.TipBannerAutoState.OnContextChanged;
    }

    // ── Public API ─────────────────────────────────────────────────

    public HelpContext CurrentContext
    {
        get { lock (_lock) return _currentContext; }
    }

    public ContextHelpResult? Resolve(string key)
    {
        // Fast path: return cached result if generation matches.
        // Volatile read of _generation is safe — it's only ever
        // incremented (inside the lock) and we accept a brief
        // window where a stale result is served during a transition.
        var gen = Volatile.Read(ref _generation);

        if (_cache.TryGetValue(key, out var cached) && cached.Generation == gen)
            return cached.Result;

        // Slow path: run the pipeline and cache the result.
        var result = RunPipeline(key);
        _cache[key] = new CacheEntry(gen, result);
        return result;
    }

    // ── Pipeline execution (called only on cache miss) ─────────────

    private ContextHelpResult? RunPipeline(string key)
    {
        var ctx = CurrentContext;
        string? suffix = null;
        string? enrichUsageTip = null;
        ContextHelpResult? content = null;

        foreach (var rule in Rules)
        {
            var result = rule.Evaluate(key, ctx);
            if (result is null)
                continue;

            // An enrichment-only result (no Description) is accumulated
            // and merged into whichever content rule fires later.
            // This covers suffix-only, tip-only, or suffix+tip results
            // (e.g. offline warnings that add both a suffix and a tip).
            if (result.Description is null)
            {
                suffix ??= result.Suffix;
                enrichUsageTip ??= result.UsageTip;
                continue;
            }

            // First content-producing rule wins
            content = result;
            break;
        }

        if (content is null && suffix is null && enrichUsageTip is null)
            return null;

        // Merge accumulated enrichments into the content result
        if (content is not null)
        {
            if (suffix is not null)
                content = content with { Suffix = suffix };
            if (enrichUsageTip is not null && content.UsageTip is null)
                content = content with { UsageTip = enrichUsageTip };

            return Adapter.Adapt(key, content, ctx.ExperienceLevel);
        }

        var fallback = new ContextHelpResult(null, enrichUsageTip, suffix);
        return Adapter.Adapt(key, fallback, ctx.ExperienceLevel);
    }

    // ── Event handlers ─────────────────────────────────────────────

    private Task OnModeChangedAsync(OperationalModeChangedEvent e)
    {
        RefreshAndPublish();
        return Task.CompletedTask;
    }

    private Task OnOfflineChangedAsync(OfflineModeChangedEvent e)
    {
        RefreshAndPublish();
        return Task.CompletedTask;
    }

    private Task OnExperienceLevelChangedAsync(ExperienceLevelPromotedEvent e)
    {
        _logger.LogDebug(
            "Experience level changed: {Previous} → {New} — refreshing help context",
            e.PreviousLevel, e.NewLevel);
        RefreshAndPublish();
        return Task.CompletedTask;
    }

    private void OnFocusLockPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IFocusLockService.IsFocusLocked)
                           or nameof(IFocusLockService.ActiveModule))
        {
            RefreshAndPublish();
        }
    }

    // ── Core refresh logic ─────────────────────────────────────────

    private void RefreshAndPublish()
    {
        HelpContext snapshot;
        lock (_lock)
        {
            _currentContext = HelpContext.From(_appState, _focusLock, _onboarding);
            snapshot = _currentContext;

            // Advance the generation counter so all cached entries
            // become stale.  The ConcurrentDictionary is NOT cleared
            // — entries are lazily replaced on next Resolve() call.
            // This avoids an O(n) bulk-clear on every state change.
            _generation++;
        }

        _logger.LogDebug(
            "HelpContext refreshed (gen {Gen}) — Mode={Mode}, Offline={Offline}, Locked={Locked}, Module={Module}, Level={Level}",
            _generation, snapshot.OperationalMode, snapshot.IsOfflineMode,
            snapshot.IsFocusLocked, snapshot.CurrentModule, snapshot.ExperienceLevel);

        PublishSafe(new HelpContextChangedEvent(snapshot));

        // Re-resolve all live context-adaptive InlineTipBanners.
        Helpers.TipBannerAutoState.ContextChangedCallback?.Invoke();
    }

    private async void PublishSafe<TEvent>(TEvent @event) where TEvent : IEvent
    {
        try
        {
            await _eventBus.PublishAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish {EventType} — event swallowed",
                typeof(TEvent).Name);
        }
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        Helpers.SmartTooltip.ContextResolver = null;
        Helpers.TipBannerAutoState.ContextResolver = null;
        Helpers.TipBannerAutoState.ContextChangedCallback = null;
        _eventBus.Unsubscribe<OperationalModeChangedEvent>(OnModeChangedAsync);
        _eventBus.Unsubscribe<OfflineModeChangedEvent>(OnOfflineChangedAsync);
        _eventBus.Unsubscribe<ExperienceLevelPromotedEvent>(OnExperienceLevelChangedAsync);
        _focusLock.PropertyChanged -= OnFocusLockPropertyChanged;
    }
}

// ═════════════════════════════════════════════════════════════════════
//  Rule interface
// ═════════════════════════════════════════════════════════════════════

/// <summary>
/// A single rule in the context-help resolution pipeline.
/// Returns a <see cref="ContextHelpResult"/> when the rule matches,
/// or <c>null</c> to defer to the next rule.
/// <para>
/// <b>To add a new rule:</b> Implement this interface, then add
/// an instance to <c>ContextHelpService.Rules</c> at the desired
/// priority position.
/// </para>
/// </summary>
internal interface IContextHelpRule
{
    ContextHelpResult? Evaluate(string key, HelpContext ctx);
}

// ═════════════════════════════════════════════════════════════════════
//  Rule 1: Offline warning  (suffix-only — appended to any match)
// ═════════════════════════════════════════════════════════════════════

/// <summary>
/// When the app is offline, emits a suffix-only result that gets
/// merged into whichever content rule fires later. This ensures
/// every tooltip gains an offline warning without duplicating text
/// in every mode-specific entry.
/// <para>
/// <b>Billing actions</b> receive a targeted message that tells the
/// cashier the sale will be queued and synced automatically, instead
/// of the generic "limited functionality" wording.
/// </para>
/// </summary>
internal sealed class OfflineWarningRule : IContextHelpRule
{
    /// <summary>
    /// Keys that represent billing-workflow actions where data is
    /// written (sales, cart, payment). These get the sync-oriented
    /// offline message instead of the generic warning.
    /// </summary>
    private static readonly HashSet<string> BillingActionKeys =
        ["NewSale", "NewBill", "Cart", "Payment", "StartBilling",
         "CompleteSale", "ResumeBilling"];

    private const string BillingSuffix =
        "⚠ Offline mode active. Changes will sync when reconnected.";

    private const string BillingTip =
        "Sales are queued locally and will sync automatically.";

    private const string GenericSuffix =
        "⚠ Limited functionality while offline.";

    public ContextHelpResult? Evaluate(string key, HelpContext ctx)
    {
        if (!ctx.IsOfflineMode)
            return null;

        if (BillingActionKeys.Contains(key))
            return new ContextHelpResult(
                Description: null,
                UsageTip: BillingTip,
                Suffix: BillingSuffix);

        return new ContextHelpResult(
            Description: null,
            UsageTip: null,
            Suffix: GenericSuffix);
    }
}

// ═════════════════════════════════════════════════════════════════════
//  Rule 2: Focus-locked override
// ═════════════════════════════════════════════════════════════════════

/// <summary>
/// When focus is locked (e.g. active billing session), provides
/// context-aware disable help explaining why actions are unavailable.
/// <para>
/// <b>Strategy — allow-list based:</b> A small set of billing-workflow
/// keys are permitted during focus lock and pass through unmodified.
/// All other keys get a description and usage tip explaining that the
/// action is locked, naming the active module (e.g. "Billing") so
/// the operator knows <em>why</em>.
/// </para>
/// <para>
/// <b>StartBilling</b> is treated specially — it is reachable (the
/// button stays enabled via <c>BillingFocusBehavior.AllowFocus</c>)
/// but the tooltip reflects the current lock state.
/// </para>
/// </summary>
internal sealed class FocusLockedRule : IContextHelpRule
{
    /// <summary>
    /// Keys that remain fully functional during focus lock.
    /// These are billing-workflow actions that the cashier needs.
    /// </summary>
    private static readonly HashSet<string> AllowedDuringLock =
        ["NewSale", "Cart", "Payment", "CompleteSale",
         "ResumeBilling", "Refresh", "FilterSales", "LoadAllSales"];

    public ContextHelpResult? Evaluate(string key, HelpContext ctx)
    {
        if (!ctx.IsFocusLocked)
            return null;

        // StartBilling button — reachable but shows lock-state help
        if (key is "StartBilling")
            return new ContextHelpResult(
                Description: "Billing mode active. Non-billing features are locked.",
                UsageTip: "Complete or cancel the current sale before stopping.",
                Suffix: null);

        // Billing-workflow keys pass through to later rules
        if (AllowedDuringLock.Contains(key))
            return null;

        // Everything else is unavailable during focus lock
        return new ContextHelpResult(
            Description: $"Unavailable during {ctx.CurrentModule} mode.",
            UsageTip: $"Complete or exit the {ctx.CurrentModule.ToLowerInvariant()} session to unlock this action.",
            Suffix: null);
    }
}

// ═════════════════════════════════════════════════════════════════════
//  Rule 3: Billing session  (active-session overrides)
// ═════════════════════════════════════════════════════════════════════

/// <summary>
/// Fires when the app is in Billing mode and the key is a
/// billing-workflow action. Provides cashier-focused guidance
/// that reflects the live session state.
/// </summary>
internal sealed class BillingSessionRule : IContextHelpRule
{
    private static readonly Dictionary<string, (string Desc, string? Tip)> Entries = new()
    {
        ["NewSale"]        = ("Add items to the cart by searching or scanning.",
                              "Use the barcode scanner for faster checkout."),
        ["Cart"]           = ("Review items, adjust quantities, and apply discounts.",
                              "Swipe left on a line item to remove it."),
        ["Payment"]        = ("Process payment and generate the receipt.",
                              "Supports cash, card, and split payments."),
        ["CompleteSale"]   = ("Finalize the sale, save the bill, and print the receipt.",
                              null),
        ["ResumeBilling"]  = ("Restore the previous billing session.",
                              "The saved cart and totals will be reloaded."),
    };

    public ContextHelpResult? Evaluate(string key, HelpContext ctx)
    {
        if (ctx.OperationalMode != OperationalMode.Billing)
            return null;

        if (!Entries.TryGetValue(key, out var entry))
            return null;

        return new ContextHelpResult(entry.Desc, entry.Tip, Suffix: null);
    }
}

// ═════════════════════════════════════════════════════════════════════
//  Rule 4: Mode-specific content
// ═════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns mode-specific help text and usage tips when a
/// <c>(key, mode)</c> entry exists in the static registries.
/// <list type="bullet">
///   <item><b>Billing mode</b> → cashier-focused: scanning, cart,
///         checkout workflow.</item>
///   <item><b>Management mode</b> → configuration-focused: catalog
///         editing, tax setup, user administration.</item>
/// </list>
/// </summary>
internal sealed class ModeSpecificRule : IContextHelpRule
{
    private static readonly Dictionary<(string, OperationalMode), string> Descriptions = new()
    {
        // ── Billing mode (cashier-focused) ──
        [("NewBill",       OperationalMode.Billing)]    = "Scan items or search products to add to the current bill.",
        [("Products",      OperationalMode.Billing)]    = "Browse the product catalog to add items to the cart.",
        [("Home",          OperationalMode.Billing)]    = "Return to the billing terminal.",
        [("ToggleMode",    OperationalMode.Billing)]    = "Exit billing mode and return to management.",
        [("Refresh",       OperationalMode.Billing)]    = "Reload product prices and tax rates.",
        [("StartBilling",  OperationalMode.Billing)]    = "Stop billing and return to management mode.",
        [("NewSale",       OperationalMode.Billing)]    = "Add items to the cart by searching or scanning.",
        [("FilterSales",   OperationalMode.Billing)]    = "Narrow the sales list to the selected date range.",
        [("LoadAllSales",  OperationalMode.Billing)]    = "Clear the date filter and display all sales.",

        // ── Management mode (configuration-focused) ──
        [("StartBilling",  OperationalMode.Management)] = "Switches app to cashier mode.",
        [("NewBill",       OperationalMode.Management)] = "Switch to billing mode to start a new sale.",
        [("Products",   OperationalMode.Management)] = "Add, edit, or remove products from the catalog.",
        [("Home",       OperationalMode.Management)] = "View the management dashboard with today's summary.",
        [("ToggleMode", OperationalMode.Management)] = "Enter billing mode for point-of-sale operations.",
        [("Firm",       OperationalMode.Management)] = "Update firm name, address, and contact details.",
        [("Users",      OperationalMode.Management)] = "Manage user accounts, roles, and PINs.",
        [("Tax",        OperationalMode.Management)] = "Configure tax profiles and rates.",
        [("Settings",   OperationalMode.Management)] = "Adjust application preferences and system settings.",
    };

    private static readonly Dictionary<(string, OperationalMode), string> Tips = new()
    {
        [("NewBill",       OperationalMode.Billing)]    = "Use the barcode scanner for faster checkout.",
        [("ToggleMode",    OperationalMode.Billing)]    = "Complete or cancel the current bill first.",
        [("StartBilling",  OperationalMode.Billing)]    = "Complete or cancel the current sale before stopping.",
        [("NewSale",       OperationalMode.Billing)]    = "Use Ctrl+N to open the new sale form.",
        [("Products",      OperationalMode.Management)] = "Use Ctrl+P to jump here from any screen.",
        [("ToggleMode",    OperationalMode.Management)] = "Billing mode locks navigation to the POS terminal.",
        [("StartBilling",  OperationalMode.Management)] = "Navigation will be locked to the billing terminal.",
    };

    public ContextHelpResult? Evaluate(string key, HelpContext ctx)
    {
        var mode = ctx.OperationalMode;
        var hasDesc = Descriptions.TryGetValue((key, mode), out var desc);
        var hasTip = Tips.TryGetValue((key, mode), out var tip);

        if (!hasDesc && !hasTip)
            return null;

        return new ContextHelpResult(desc, tip, Suffix: null);
    }
}

// ═════════════════════════════════════════════════════════════════════
//  Rule 5: Fallback (mode-agnostic)
// ═════════════════════════════════════════════════════════════════════

/// <summary>
/// Catches any key that wasn't matched by a mode-specific rule.
/// Returns mode-agnostic descriptions and tips that apply regardless
/// of the current operational mode.
/// </summary>
internal sealed class FallbackRule : IContextHelpRule
{
    private static readonly Dictionary<string, string> Descriptions = new()
    {
        ["Tasks"]         = "View pending tasks and reminders.",
        ["Refresh"]       = "Reload the current view data.",
        ["Logout"]        = "Sign out and return to the login screen.",
        ["NewSale"]       = "Start a new sale transaction.",
        ["Cart"]          = "Add items to the current sale.",
        ["CompleteSale"]  = "Complete the sale and generate a receipt.",
        ["FilterSales"]   = "Filter the sales list by date range.",
        ["LoadAllSales"]  = "Clear the date filter and show all sales.",
    };

    private static readonly Dictionary<string, string> Tips = new()
    {
        ["Global"] = "Hover over toolbar buttons for help.",
    };

    public ContextHelpResult? Evaluate(string key, HelpContext ctx)
    {
        var hasDesc = Descriptions.TryGetValue(key, out var desc);
        var hasTip = Tips.TryGetValue(key, out var tip);

        if (!hasDesc && !hasTip)
            return null;

        return new ContextHelpResult(desc, tip, Suffix: null);
    }
}

// ═════════════════════════════════════════════════════════════════════
//  Experience-level post-processor
// ═════════════════════════════════════════════════════════════════════

/// <summary>
/// Post-processes a <see cref="ContextHelpResult"/> based on the
/// operator's <see cref="UserExperienceLevel"/>, adjusting both the
/// description and usage tip to match the expected verbosity.
///
/// <para><b>Beginner</b> — detailed, step-by-step text that assumes
/// no prior knowledge of the application. If a beginner-specific
/// override exists for the given key, it replaces the standard text.
/// Otherwise the standard result is returned as-is (it is already
/// reasonably descriptive).</para>
///
/// <para><b>Intermediate</b> — the default rule output, unchanged.
/// This is the baseline that every existing rule produces today.</para>
///
/// <para><b>Advanced</b> — terse, action-oriented text. If an
/// advanced-specific override exists, it replaces the standard text.
/// If not, the standard result is returned as-is.</para>
///
/// <para><b>Design:</b> Overrides are stored in flat dictionaries
/// keyed by help key. Only keys that benefit from level-specific
/// wording need an entry — all others fall through unchanged. This
/// keeps the adapter lightweight (no allocation on the common path)
/// while allowing per-key fine-tuning.</para>
/// </summary>
internal sealed class ExperienceLevelAdapter
{
    // ── Beginner overrides (detailed, step-by-step) ────────────

    private static readonly Dictionary<string, (string Desc, string? Tip)> BeginnerOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NewSale"] = (
            "Start a new sale — search for products by name or scan a barcode. Items are added to your cart where you can adjust quantities before completing the transaction.",
            "Press Ctrl+N to open the new sale form quickly. Use the barcode scanner for even faster entry."),
        ["Cart"] = (
            "Your shopping cart shows all items for the current sale. You can adjust quantities, remove items, or apply discounts here before completing the sale.",
            "Select a line item and press Delete to remove it. Click the quantity to change it."),
        ["Payment"] = (
            "Process payment for the current sale. You can accept cash, card, or split across multiple payment methods.",
            "The system calculates change automatically for cash payments."),
        ["CompleteSale"] = (
            "Finalize the sale — this saves the bill, prints a receipt, and clears the cart so you can start the next customer.",
            "Make sure all items and quantities are correct before completing."),
        ["Products"] = (
            "Open the product catalog to add, edit, or remove products. Each product needs a name, a price, and a tax category to appear in billing.",
            "Press Ctrl+N to add a new product. Use the search bar to find existing products quickly."),
        ["Home"] = (
            "Return to the main dashboard. The dashboard shows today's sales summary, recent activity, and quick links to common actions.",
            null),
        ["StartBilling"] = (
            "Switch to billing mode — this locks the screen to the point-of-sale terminal so you can focus on serving customers without accidental navigation.",
            "Navigation to other sections is locked during billing for safety."),
        ["ToggleMode"] = (
            "Switch between Management mode (catalog, settings, reports) and Billing mode (point-of-sale terminal).",
            "In Billing mode, navigation is locked to the POS terminal."),
        ["Firm"] = (
            "Update your store details — firm name, address, contact information, and GST/tax registration numbers that appear on receipts.",
            null),
        ["Users"] = (
            "Manage who can access the system. Add new users, assign roles (Admin, Manager, User), set PINs, and deactivate accounts.",
            null),
        ["Tax"] = (
            "Set up tax categories and rates that apply to your products. Each product is linked to a tax category so the correct tax is calculated automatically.",
            null),
        ["Settings"] = (
            "Adjust application preferences — general settings, backup schedule, security options, and system information.",
            null),
        ["Tasks"] = (
            "View your pending tasks and reminders. Tasks help you track things like restocking, follow-ups, and scheduled maintenance.",
            null),
        ["Refresh"] = (
            "Reload all data from the database. Use this if you think the screen might be showing stale information.",
            null),
        ["Logout"] = (
            "Sign out of the application and return to the login screen. Any unsaved changes will be lost.",
            "Make sure to complete or cancel any active billing session before logging out."),
        ["FilterSales"] = (
            "Narrow the sales list to show only transactions within your selected date range. This helps you find specific sales quickly.",
            null),
        ["LoadAllSales"] = (
            "Remove the date filter and show every sale in the system. Useful when you need to search across all dates.",
            null),
        ["ResumeBilling"] = (
            "Restore your previous billing session — the saved cart, items, and totals will be reloaded exactly as you left them.",
            "This appears when the app detects an incomplete session from a previous run."),
    };

    // ── Advanced overrides (concise, action-oriented) ──────────

    private static readonly Dictionary<string, (string Desc, string? Tip)> AdvancedOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NewSale"]       = ("New sale.", "F5"),
        ["Cart"]          = ("Edit cart.", null),
        ["Payment"]       = ("Process payment.", null),
        ["CompleteSale"]  = ("Finalize and print receipt.", null),
        ["Products"]      = ("Product catalog.", "Ctrl+P"),
        ["Home"]          = ("Dashboard.", null),
        ["StartBilling"]  = ("Toggle billing mode.", null),
        ["ToggleMode"]    = ("Switch mode.", null),
        ["Firm"]          = ("Firm details.", null),
        ["Users"]         = ("User management.", null),
        ["Tax"]           = ("Tax configuration.", null),
        ["Settings"]      = ("System settings.", null),
        ["Tasks"]         = ("Tasks.", null),
        ["Refresh"]       = ("Reload data.", null),
        ["Logout"]        = ("Sign out.", null),
        ["FilterSales"]   = ("Filter by date.", null),
        ["LoadAllSales"]  = ("Show all sales.", null),
        ["ResumeBilling"] = ("Resume session.", null),
    };

    /// <summary>
    /// Adapts a pipeline-produced <see cref="ContextHelpResult"/>
    /// based on the operator's experience level.
    /// <para>
    /// Returns the <paramref name="result"/> unchanged when no
    /// override exists for the level or when the level is
    /// <see cref="UserExperienceLevel.Intermediate"/>.
    /// </para>
    /// </summary>
    public ContextHelpResult? Adapt(
        string key, ContextHelpResult? result, UserExperienceLevel level)
    {
        if (result is null)
            return null;

        var overrides = level switch
        {
            UserExperienceLevel.Beginner => BeginnerOverrides,
            UserExperienceLevel.Advanced => AdvancedOverrides,
            _ => null,
        };

        if (overrides is null || !overrides.TryGetValue(key, out var entry))
            return result;

        // Preserve the Suffix from earlier pipeline stages (e.g. offline
        // warnings) — only replace the Description and UsageTip.
        return result with
        {
            Description = entry.Desc,
            UsageTip = entry.Tip ?? result.UsageTip,
        };
    }
}
