using System.Windows;
using StoreAssistantPro.Core.Controls;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that automatically wires an <see cref="InlineTipBanner"/>
/// to <see cref="Services.ITipStateService"/> (dismiss persistence) and
/// <see cref="Services.IContextHelpService"/> (context-adaptive text).
///
/// <para><b>Persistence (TipKey):</b></para>
/// <list type="number">
///   <item>Set <c>h:TipBannerAutoState.TipKey</c> on any
///         <see cref="InlineTipBanner"/>.</item>
///   <item>On <c>Loaded</c>, the behavior checks
///         <see cref="IsDismissedResolver"/> and sets
///         <c>IsDismissed = true</c> if the tip was previously
///         dismissed.</item>
///   <item>On dismiss, <see cref="DismissFunc"/> persists the key to
///         the settings file.</item>
/// </list>
///
/// <para><b>Context adaptation (ContextKey):</b></para>
/// <list type="number">
///   <item>Set <c>h:TipBannerAutoState.ContextKey</c> on the same
///         banner. The key is the same format used by
///         <see cref="SmartTooltip.ContextKeyProperty"/> (e.g.
///         <c>"NewSale"</c>, <c>"Products"</c>).</item>
///   <item>On <c>Loaded</c>, and again whenever <see cref="ContextResolver"/>
///         signals a context change via <see cref="ContextChangedCallback"/>,
///         the behavior resolves the <see cref="ContextHelpResult"/>
///         and overwrites <see cref="InlineTipBanner.Title"/> and
///         <see cref="InlineTipBanner.TipText"/> with the context-specific
///         text.  When no rule matches, the original XAML-authored values
///         are restored.</item>
///   <item>This means the same banner automatically shows:
///     <list type="bullet">
///       <item>Billing-specific tips when in Billing mode.</item>
///       <item>Offline warning tips when connectivity is lost.</item>
///       <item>Management-focused tips when in Management mode.</item>
///     </list>
///   </item>
/// </list>
///
/// <para><b>MainWindow exclusion:</b> The behavior operates on individual
/// banners, not on windows.  To exclude MainWindow, simply don't place
/// any banners with <c>TipKey</c> inside it.</para>
///
/// <para><b>Service coupling:</b> This static class avoids a direct
/// reference to any DI service.  Static delegates are wired at startup
/// by <see cref="Services.TipStateService"/> and
/// <see cref="Services.ContextHelpService"/> (identical to the
/// <see cref="SmartTooltip.ContextResolver"/> pattern).</para>
///
/// <para><b>Usage (XAML):</b></para>
/// <code>
/// &lt;!-- Static tip (persist-only, no context adaptation) --&gt;
/// &lt;controls:InlineTipBanner
///     h:TipBannerAutoState.TipKey="ProductsView.AddProductTip"
///     Title="Quick tip"
///     TipText="Use Ctrl+N to add a product from any screen."/&gt;
///
/// &lt;!-- Context-adaptive tip (text changes with mode/offline) --&gt;
/// &lt;controls:InlineTipBanner
///     h:TipBannerAutoState.TipKey="SalesView.NewSaleTip"
///     h:TipBannerAutoState.ContextKey="NewSale"
///     Title="Quick tip"
///     TipText="Start a new sale transaction."/&gt;
/// </code>
/// </summary>
public static class TipBannerAutoState
{
    // ── Static delegates (wired at app startup) ─────────────────────

    /// <summary>
    /// Checks whether a tip has been dismissed.
    /// Wired by <see cref="Services.TipStateService"/> constructor.
    /// </summary>
    public static Func<string, bool>? IsDismissedResolver { get; set; }

    /// <summary>
    /// Persists a tip dismissal.
    /// Wired by <see cref="Services.TipStateService"/> constructor.
    /// </summary>
    public static Action<string>? DismissFunc { get; set; }

    /// <summary>
    /// Resolves context-aware help text for a given key.
    /// Wired by <see cref="Services.ContextHelpService"/> constructor
    /// (same delegate as <see cref="SmartTooltip.ContextResolver"/>).
    /// </summary>
    public static Func<string, ContextHelpResult?>? ContextResolver { get; set; }

    /// <summary>
    /// Invoked by <see cref="Services.ContextHelpService"/> when the
    /// application context changes (mode switch, offline transition,
    /// focus lock change).  The behavior re-resolves all live banners
    /// that have a <see cref="ContextKeyProperty"/>.
    /// <para>
    /// <b>Wiring:</b> <c>ContextHelpService</c> calls this delegate
    /// from its <c>RefreshAndPublish</c> method — no event-bus
    /// subscription needed in this static class.
    /// </para>
    /// </summary>
    public static Action? ContextChangedCallback { get; set; }

    // ── TipKey attached property ────────────────────────────────────

    /// <summary>
    /// Unique tip identifier persisted by <see cref="Services.ITipStateService"/>.
    /// Setting this property activates the dismiss-persistence behavior.
    /// Use a <c>ViewName.TipName</c> format, e.g.
    /// <c>"ProductsView.AddProductTip"</c>.
    /// </summary>
    public static readonly DependencyProperty TipKeyProperty =
        DependencyProperty.RegisterAttached(
            "TipKey",
            typeof(string),
            typeof(TipBannerAutoState),
            new PropertyMetadata(null, OnTipKeyChanged));

    public static string? GetTipKey(DependencyObject obj) =>
        (string?)obj.GetValue(TipKeyProperty);

    public static void SetTipKey(DependencyObject obj, string? value) =>
        obj.SetValue(TipKeyProperty, value);

    // ── ContextKey attached property ────────────────────────────────

    /// <summary>
    /// Help key used to resolve context-aware text from
    /// <see cref="ContextResolver"/>. When set, the behavior
    /// overwrites <see cref="InlineTipBanner.Title"/> and
    /// <see cref="InlineTipBanner.TipText"/> with context-specific
    /// text at load time and on every context change.
    /// <para>
    /// Uses the same key namespace as
    /// <see cref="SmartTooltip.ContextKeyProperty"/> (e.g.
    /// <c>"NewSale"</c>, <c>"Products"</c>).
    /// </para>
    /// </summary>
    public static readonly DependencyProperty ContextKeyProperty =
        DependencyProperty.RegisterAttached(
            "ContextKey",
            typeof(string),
            typeof(TipBannerAutoState),
            new PropertyMetadata(null, OnContextKeyChanged));

    public static string? GetContextKey(DependencyObject obj) =>
        (string?)obj.GetValue(ContextKeyProperty);

    public static void SetContextKey(DependencyObject obj, string? value) =>
        obj.SetValue(ContextKeyProperty, value);

    // ── Private: original-text stash (restore when no rule matches) ──

    private static readonly DependencyProperty OriginalTitleProperty =
        DependencyProperty.RegisterAttached(
            "OriginalTitle",
            typeof(string),
            typeof(TipBannerAutoState),
            new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalTipTextProperty =
        DependencyProperty.RegisterAttached(
            "OriginalTipText",
            typeof(string),
            typeof(TipBannerAutoState),
            new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalsCapturedProperty =
        DependencyProperty.RegisterAttached(
            "OriginalsCaptured",
            typeof(bool),
            typeof(TipBannerAutoState),
            new PropertyMetadata(false));

    // ── Live context-banners registry ───────────────────────────────
    // WeakReferences to all banners that have a ContextKey and are
    // currently loaded. On context change, we iterate this set and
    // re-resolve each one.

    private static readonly List<WeakReference<InlineTipBanner>> LiveContextBanners = [];
    private static readonly Lock LiveBannersLock = new();

    // ── TipKey wiring ───────────────────────────────────────────────

    private static void OnTipKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not InlineTipBanner banner)
            return;

        banner.Loaded -= OnBannerLoaded;
        banner.RemoveHandler(InlineTipBanner.DismissedEvent,
            (RoutedEventHandler)OnBannerDismissed);

        if (e.NewValue is string key && key.Length > 0)
        {
            banner.Loaded += OnBannerLoaded;
            banner.AddHandler(InlineTipBanner.DismissedEvent,
                (RoutedEventHandler)OnBannerDismissed);
        }
    }

    // ── ContextKey wiring ───────────────────────────────────────────

    private static void OnContextKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not InlineTipBanner banner)
            return;

        banner.Loaded -= OnContextBannerLoaded;
        banner.Unloaded -= OnContextBannerUnloaded;
        UnregisterLiveBanner(banner);

        if (e.NewValue is string key && key.Length > 0)
        {
            banner.Loaded += OnContextBannerLoaded;
            banner.Unloaded += OnContextBannerUnloaded;

            if (banner.IsLoaded)
            {
                CaptureOriginalsIfNeeded(banner);
                ResolveAndApplyContext(banner);
                RegisterLiveBanner(banner);
            }
        }
        else if ((bool)banner.GetValue(OriginalsCapturedProperty))
        {
            RestoreOriginals(banner);
        }
    }

    // ── Loaded: dismiss check ───────────────────────────────────────

    private static void OnBannerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not InlineTipBanner banner)
            return;

        var key = GetTipKey(banner);
        if (string.IsNullOrEmpty(key))
            return;

        if (IsDismissedResolver?.Invoke(key) == true)
            banner.IsDismissed = true;
    }

    // ── Loaded: context resolution + registration ───────────────────

    private static void OnContextBannerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not InlineTipBanner banner)
            return;

        CaptureOriginalsIfNeeded(banner);

        ResolveAndApplyContext(banner);
        RegisterLiveBanner(banner);
    }

    // ── Unloaded: deregistration ────────────────────────────────────

    private static void OnContextBannerUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not InlineTipBanner banner)
            return;

        UnregisterLiveBanner(banner);
    }

    // ── Dismissed: persist ──────────────────────────────────────────

    private static void OnBannerDismissed(object sender, RoutedEventArgs e)
    {
        if (sender is not InlineTipBanner banner)
            return;

        var key = GetTipKey(banner);
        if (string.IsNullOrEmpty(key))
            return;

        DismissFunc?.Invoke(key);
    }

    // ── Context resolution ──────────────────────────────────────────

    /// <summary>
    /// Resolves the <see cref="ContextHelpResult"/> for the banner's
    /// <see cref="ContextKeyProperty"/> and applies it to
    /// <see cref="InlineTipBanner.TipText"/>.  Falls back to the
    /// original XAML-authored values when no rule matches.
    /// </summary>
    private static void ResolveAndApplyContext(InlineTipBanner banner)
    {
        var contextKey = GetContextKey(banner);
        if (string.IsNullOrEmpty(contextKey))
            return;

        var result = ContextResolver?.Invoke(contextKey);
        var originalTitle = (string?)banner.GetValue(OriginalTitleProperty);
        var originalText = (string?)banner.GetValue(OriginalTipTextProperty);

        if (result is not null)
        {
            if (originalTitle is not null)
                banner.Title = originalTitle;

            var tipText = result.EffectiveDescription ?? originalText ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(result.UsageTip))
                tipText = string.IsNullOrWhiteSpace(tipText)
                    ? result.UsageTip
                    : $"{tipText} {result.UsageTip}";

            banner.TipText = tipText;
        }
        else
        {
            RestoreOriginals(banner);
        }
    }

    private static void CaptureOriginalsIfNeeded(InlineTipBanner banner)
    {
        if ((bool)banner.GetValue(OriginalsCapturedProperty))
            return;

        banner.SetValue(OriginalTitleProperty, banner.Title);
        banner.SetValue(OriginalTipTextProperty, banner.TipText);
        banner.SetValue(OriginalsCapturedProperty, true);
    }

    private static void RestoreOriginals(InlineTipBanner banner)
    {
        if (banner.GetValue(OriginalTitleProperty) is string originalTitle)
            banner.Title = originalTitle;

        if (banner.GetValue(OriginalTipTextProperty) is string originalText)
            banner.TipText = originalText;
    }

    // ── Context changed: re-resolve all live banners ────────────────

    /// <summary>
    /// Called by <see cref="Services.ContextHelpService"/> when the
    /// application context changes. Iterates all live context banners
    /// and re-resolves their text. Dead references are pruned.
    /// </summary>
    internal static void OnContextChanged()
    {
        List<InlineTipBanner> alive;
        lock (LiveBannersLock)
        {
            alive = new List<InlineTipBanner>(LiveContextBanners.Count);
            for (var i = LiveContextBanners.Count - 1; i >= 0; i--)
            {
                if (LiveContextBanners[i].TryGetTarget(out var banner))
                    alive.Add(banner);
                else
                    LiveContextBanners.RemoveAt(i); // prune dead
            }
        }

        foreach (var banner in alive)
        {
            // Must dispatch to UI thread — context changes can arrive
            // from background event handlers.
            if (banner.Dispatcher.CheckAccess())
                ResolveAndApplyContext(banner);
            else
                banner.Dispatcher.BeginInvoke(() => ResolveAndApplyContext(banner));
        }
    }

    // ── Live banner registry ────────────────────────────────────────

    private static void RegisterLiveBanner(InlineTipBanner banner)
    {
        lock (LiveBannersLock)
        {
            // Avoid duplicates (banner re-loaded without unload)
            for (var i = LiveContextBanners.Count - 1; i >= 0; i--)
            {
                if (!LiveContextBanners[i].TryGetTarget(out var existing))
                {
                    LiveContextBanners.RemoveAt(i); // prune dead
                    continue;
                }
                if (ReferenceEquals(existing, banner))
                    return; // already registered
            }

            LiveContextBanners.Add(new WeakReference<InlineTipBanner>(banner));
        }
    }

    private static void UnregisterLiveBanner(InlineTipBanner banner)
    {
        lock (LiveBannersLock)
        {
            for (var i = LiveContextBanners.Count - 1; i >= 0; i--)
            {
                if (!LiveContextBanners[i].TryGetTarget(out var existing)
                    || ReferenceEquals(existing, banner))
                {
                    LiveContextBanners.RemoveAt(i);
                }
            }
        }
    }
}
