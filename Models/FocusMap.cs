namespace StoreAssistantPro.Models;

/// <summary>
/// Declares the logical focus order for a single context (page, dialog, or form).
/// <para>
/// <b>FocusMap is immutable data, not UI logic.</b> It describes <em>what</em>
/// elements exist and their intended order — the
/// <see cref="Core.Services.IPredictiveFocusService"/> decides <em>when</em>
/// and <em>where</em> to move focus based on the map.
/// </para>
///
/// <para><b>Registration (module startup):</b></para>
/// <code>
/// focusMapRegistry.Register(FocusMap.For("Vendors")
///     .Add("NameInput",    FocusRole.PrimaryInput)
///     .Add("ContactInput", FocusRole.FormField)
///     .Add("SaveButton",   FocusRole.PrimaryAction)
///     .Build());
/// </code>
///
/// <para><b>Query (PredictiveFocusService):</b></para>
/// <code>
/// var map = focusMapRegistry.Get("Vendors");
/// var landing = map?.GetLandingTarget();           // → "NameInput"
/// var next    = map?.GetNextElement("NameInput");   // → "ContactInput"
/// var save    = map?.GetByRole(FocusRole.PrimaryAction); // → "SaveButton"
/// </code>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Pure data — no WPF types, no service dependencies.</item>
///   <item>Immutable after <see cref="Builder.Build"/> — thread-safe.</item>
///   <item>One map per context key (page key, dialog key, or form name).</item>
///   <item>Entries are ordered — index 0 is the default landing target.</item>
/// </list>
/// </summary>
public sealed class FocusMap
{
    /// <summary>
    /// The context key this map belongs to (e.g., <c>"Vendors"</c>,
    /// <c>"FirmManagement"</c>, <c>"BillingForm"</c>).
    /// Must match the page key, dialog key, or a module-defined form name.
    /// </summary>
    public string ContextKey { get; }

    /// <summary>
    /// Ordered list of focusable elements. Index 0 is the default
    /// landing target. The order defines the logical Tab sequence.
    /// </summary>
    public IReadOnlyList<FocusMapEntry> Entries { get; }

    /// <summary>Fast lookup: element name → index in <see cref="Entries"/>.</summary>
    private readonly Dictionary<string, int> _indexByName;

    private FocusMap(string contextKey, IReadOnlyList<FocusMapEntry> entries)
    {
        ContextKey = contextKey;
        Entries = entries;

        _indexByName = new Dictionary<string, int>(entries.Count, StringComparer.Ordinal);
        for (var i = 0; i < entries.Count; i++)
            _indexByName[entries[i].ElementName] = i;
    }

    // ── Query API ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the default landing target (first entry), or <c>null</c>
    /// if the map is empty.
    /// </summary>
    public string? GetLandingTarget() =>
        Entries.Count > 0 ? Entries[0].ElementName : null;

    /// <summary>
    /// Returns the first entry with the specified <paramref name="role"/>,
    /// or <c>null</c> if none exists.
    /// </summary>
    public FocusMapEntry? GetByRole(FocusRole role) =>
        Entries.FirstOrDefault(e => e.Role == role);

    /// <summary>
    /// Returns the element name after <paramref name="currentElement"/>
    /// in the logical order, or <c>null</c> if it's the last entry
    /// or the element is not found.
    /// </summary>
    public string? GetNextElement(string currentElement)
    {
        if (!_indexByName.TryGetValue(currentElement, out var idx))
            return null;

        var next = idx + 1;
        return next < Entries.Count ? Entries[next].ElementName : null;
    }

    /// <summary>
    /// Returns the element name before <paramref name="currentElement"/>
    /// in the logical order, or <c>null</c> if it's the first entry
    /// or the element is not found.
    /// </summary>
    public string? GetPreviousElement(string currentElement)
    {
        if (!_indexByName.TryGetValue(currentElement, out var idx))
            return null;

        var prev = idx - 1;
        return prev >= 0 ? Entries[prev].ElementName : null;
    }

    /// <summary>
    /// Returns <c>true</c> if the map contains an entry with the
    /// specified <paramref name="elementName"/>.
    /// </summary>
    public bool Contains(string elementName) =>
        _indexByName.ContainsKey(elementName);

    /// <summary>
    /// Returns all entries with the specified <paramref name="role"/>.
    /// </summary>
    public IEnumerable<FocusMapEntry> GetAllByRole(FocusRole role) =>
        Entries.Where(e => e.Role == role);

    // ── Builder ──────────────────────────────────────────────────────

    /// <summary>Start building a FocusMap for the given context key.</summary>
    public static Builder For(string contextKey) => new(contextKey);

    /// <summary>
    /// Fluent builder for constructing an immutable <see cref="FocusMap"/>.
    /// </summary>
    public sealed class Builder
    {
        private readonly string _contextKey;
        private readonly List<FocusMapEntry> _entries = [];
        private readonly HashSet<string> _names = new(StringComparer.Ordinal);

        internal Builder(string contextKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
            _contextKey = contextKey;
        }

        /// <summary>
        /// Add a focusable element to the logical order.
        /// </summary>
        /// <param name="elementName">The <c>x:Name</c> of the element.</param>
        /// <param name="role">The workflow role.</param>
        /// <param name="isAvailableWhen">Optional guard property name.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="elementName"/> is already in the map.
        /// </exception>
        public Builder Add(string elementName, FocusRole role, string? isAvailableWhen = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);

            if (!_names.Add(elementName))
                throw new InvalidOperationException(
                    $"Element '{elementName}' is already registered in FocusMap '{_contextKey}'.");

            _entries.Add(new FocusMapEntry(elementName, role, isAvailableWhen));
            return this;
        }

        /// <summary>
        /// Build the immutable <see cref="FocusMap"/>.
        /// </summary>
        public FocusMap Build() => new(_contextKey, _entries.AsReadOnly());
    }
}
