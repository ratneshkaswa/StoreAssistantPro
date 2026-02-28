using System.Collections.Concurrent;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Thread-safe singleton implementation of <see cref="IFocusMapRegistry"/>.
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> internally so
/// modules can register maps from any thread during startup, while
/// runtime queries from the UI thread are lock-free.
/// </para>
/// </summary>
public sealed class FocusMapRegistry : IFocusMapRegistry
{
    private readonly ConcurrentDictionary<string, FocusMap> _maps = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public void Register(FocusMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        _maps[map.ContextKey] = map;
    }

    /// <inheritdoc/>
    public FocusMap? Get(string contextKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
        return _maps.GetValueOrDefault(contextKey);
    }

    /// <inheritdoc/>
    public bool Contains(string contextKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
        return _maps.ContainsKey(contextKey);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetRegisteredKeys() =>
        _maps.Keys.ToArray();
}
