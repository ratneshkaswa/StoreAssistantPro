using System.Collections.Concurrent;
using System.Windows;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Semantic help metadata attached to any <see cref="FrameworkElement"/>.
/// Automatically bridges to <see cref="SmartTooltip"/> for display,
/// opting into the shared TeachingTip-like callout chrome, and
/// maintains a global registry for discoverability (e.g. a future help
/// panel or keyboard shortcut overlay).
///
/// <para><b>Properties:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Property</term>
///     <description>Purpose</description>
///   </listheader>
///   <item>
///     <term><see cref="HelpTextProperty"/></term>
///     <description>Descriptive help text shown in the tooltip body.
///     Maps to <c>SmartTooltip.Text</c>.</description>
///   </item>
///   <item>
///     <term><see cref="ShortcutTextProperty"/></term>
///     <description>Optional keyboard shortcut (e.g. "Ctrl+N").
///     Shown as the bold tooltip header line.
///     Maps to <c>SmartTooltip.Header</c>.</description>
///   </item>
///   <item>
///     <term><see cref="CategoryProperty"/></term>
///     <description>Logical grouping (e.g. "Sales", "Navigation",
///     "Admin"). Stored as metadata — no tooltip effect, but
///     queryable via <see cref="GetRegisteredHints"/>.</description>
///   </item>
/// </list>
///
/// <para><b>Usage (XAML):</b></para>
/// <code>
/// &lt;!-- Simple help text → tooltip body --&gt;
/// &lt;Button h:HelpHint.HelpText="Save changes" … /&gt;
///
/// &lt;!-- With keyboard shortcut → tooltip header + body --&gt;
/// &lt;Button h:HelpHint.ShortcutText="Ctrl+S"
///         h:HelpHint.HelpText="Save all pending changes"
///         h:HelpHint.Category="File" … /&gt;
///
/// &lt;!-- Categorized for discoverability --&gt;
/// &lt;Button h:HelpHint.HelpText="Start a new sale"
///         h:HelpHint.ShortcutText="Ctrl+N"
///         h:HelpHint.Category="Sales" … /&gt;
/// </code>
///
/// <para><b>Querying registered hints:</b></para>
/// <code>
/// // Get all registered help entries (e.g. for a help overlay)
/// var allHints = HelpHint.GetRegisteredHints();
/// var salesHints = allHints.Where(h =&gt; h.Category == "Sales");
/// </code>
/// </summary>
public static class HelpHint
{
    // ═════════════════════════════════════════════════════════════════
    //  Global registry  —  tracks all annotated elements
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Thread-safe registry of every element that has at least
    /// <see cref="HelpTextProperty"/> set. Entries are added on
    /// <c>Loaded</c> and removed on <c>Unloaded</c> to keep the
    /// registry in sync with the visual tree lifetime.
    /// </summary>
    private static readonly ConcurrentDictionary<int, HelpEntry> Registry = new();

    /// <summary>Monotonic ID for registry entries.</summary>
    private static int _nextId;

    // ═════════════════════════════════════════════════════════════════
    //  HELP TEXT  —  tooltip body
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty HelpTextProperty =
        DependencyProperty.RegisterAttached(
            "HelpText", typeof(string), typeof(HelpHint),
            new PropertyMetadata(null, OnHelpPropertyChanged));

    public static string? GetHelpText(DependencyObject obj) =>
        (string?)obj.GetValue(HelpTextProperty);

    public static void SetHelpText(DependencyObject obj, string? value) =>
        obj.SetValue(HelpTextProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  SHORTCUT TEXT  —  tooltip header  (optional)
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty ShortcutTextProperty =
        DependencyProperty.RegisterAttached(
            "ShortcutText", typeof(string), typeof(HelpHint),
            new PropertyMetadata(null, OnHelpPropertyChanged));

    public static string? GetShortcutText(DependencyObject obj) =>
        (string?)obj.GetValue(ShortcutTextProperty);

    public static void SetShortcutText(DependencyObject obj, string? value) =>
        obj.SetValue(ShortcutTextProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  CATEGORY  —  logical grouping  (optional)
    // ═════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.RegisterAttached(
            "Category", typeof(string), typeof(HelpHint),
            new PropertyMetadata(null, OnHelpPropertyChanged));

    public static string? GetCategory(DependencyObject obj) =>
        (string?)obj.GetValue(CategoryProperty);

    public static void SetCategory(DependencyObject obj, string? value) =>
        obj.SetValue(CategoryProperty, value);

    // ═════════════════════════════════════════════════════════════════
    //  Internal — registry entry ID stored on each element
    // ═════════════════════════════════════════════════════════════════

    private static readonly DependencyProperty EntryIdProperty =
        DependencyProperty.RegisterAttached(
            "EntryId", typeof(int), typeof(HelpHint),
            new PropertyMetadata(-1));

    // ═════════════════════════════════════════════════════════════════
    //  Internal — subscription guard
    // ═════════════════════════════════════════════════════════════════

    private static readonly DependencyProperty SubscribedProperty =
        DependencyProperty.RegisterAttached(
            "HelpHintSubscribed", typeof(bool), typeof(HelpHint),
            new PropertyMetadata(false));

    // ═════════════════════════════════════════════════════════════════
    //  Property-changed callback  —  bridge to SmartTooltip + registry
    // ═════════════════════════════════════════════════════════════════

    private static void OnHelpPropertyChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        var helpText = GetHelpText(fe);
        var shortcutText = GetShortcutText(fe);

        // ── Bridge to SmartTooltip ──────────────────────────────
        // SmartTooltip handles all hover-delay, anti-flicker, and
        // Fluent-styled rendering.  We simply forward the metadata.
        SmartTooltip.SetText(fe, helpText);
        SmartTooltip.SetHeader(fe, shortcutText);
        SmartTooltip.SetUseCalloutStyle(fe, helpText is not null);

        // ── Lifecycle hooks for registry ────────────────────────
        var isSubscribed = (bool)fe.GetValue(SubscribedProperty);

        if (helpText is not null && !isSubscribed)
        {
            fe.Loaded += OnLoaded;
            fe.Unloaded += OnUnloaded;
            fe.SetValue(SubscribedProperty, true);

            // If already loaded (property set after initial load),
            // register immediately.
            if (fe.IsLoaded)
                RegisterElement(fe);
        }
        else if (helpText is null && isSubscribed)
        {
            UnregisterElement(fe);
            fe.Loaded -= OnLoaded;
            fe.Unloaded -= OnUnloaded;
            fe.SetValue(SubscribedProperty, false);
        }
        else if (helpText is not null && isSubscribed && fe.IsLoaded)
        {
            // Update an existing registration in-place
            UpdateRegistration(fe);
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Lifecycle handlers
    // ═════════════════════════════════════════════════════════════════

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
            RegisterElement(fe);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
            UnregisterElement(fe);
    }

    // ═════════════════════════════════════════════════════════════════
    //  Registry operations
    // ═════════════════════════════════════════════════════════════════

    private static void RegisterElement(FrameworkElement fe)
    {
        // Avoid double-registration
        var existingId = (int)fe.GetValue(EntryIdProperty);
        if (existingId >= 0 && Registry.ContainsKey(existingId))
        {
            UpdateRegistration(fe);
            return;
        }

        var id = Interlocked.Increment(ref _nextId);
        fe.SetValue(EntryIdProperty, id);

        Registry[id] = BuildEntry(fe, id);
    }

    private static void UpdateRegistration(FrameworkElement fe)
    {
        var id = (int)fe.GetValue(EntryIdProperty);
        if (id >= 0)
            Registry[id] = BuildEntry(fe, id);
    }

    private static void UnregisterElement(FrameworkElement fe)
    {
        var id = (int)fe.GetValue(EntryIdProperty);
        if (id >= 0)
        {
            Registry.TryRemove(id, out _);
            fe.SetValue(EntryIdProperty, -1);
        }
    }

    private static HelpEntry BuildEntry(FrameworkElement fe, int id) => new(
        Id: id,
        HelpText: GetHelpText(fe) ?? string.Empty,
        ShortcutText: GetShortcutText(fe),
        Category: GetCategory(fe),
        ElementType: fe.GetType().Name,
        ElementName: fe.Name);

    // ═════════════════════════════════════════════════════════════════
    //  Public query API
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a snapshot of all currently registered help entries.
    /// Useful for building a help overlay, cheat-sheet dialog, or
    /// accessibility report.
    /// </summary>
    public static IReadOnlyList<HelpEntry> GetRegisteredHints() =>
        Registry.Values.ToList().AsReadOnly();

    /// <summary>
    /// Returns registered help entries filtered by category.
    /// </summary>
    public static IReadOnlyList<HelpEntry> GetRegisteredHints(string category) =>
        Registry.Values
            .Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();

    // ═════════════════════════════════════════════════════════════════
    //  Help entry record
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Immutable snapshot of a single help-hint registration.
    /// </summary>
    public sealed record HelpEntry(
        int Id,
        string HelpText,
        string? ShortcutText,
        string? Category,
        string ElementType,
        string? ElementName);
}
