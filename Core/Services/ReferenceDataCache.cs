using System.Collections.Concurrent;

namespace StoreAssistantPro.Core.Services;

public sealed class ReferenceDataCache : IReferenceDataCache
{
    private sealed record CacheEntry(object Value, DateTimeOffset ExpiresAtUtc);

    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<T> GetOrCreateValueAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        if (TryGetValidEntry(key, out T cached))
            return cached;

        var gate = _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (TryGetValidEntry(key, out cached))
                return cached;

            var created = await factory(ct).ConfigureAwait(false);
            _entries[key] = new CacheEntry(created!, DateTimeOffset.UtcNow.Add(ttl));
            return created;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<T>> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<IReadOnlyList<T>>> factory,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        return await GetOrCreateValueAsync(
            key,
            async innerCt =>
            {
                var created = await factory(innerCt).ConfigureAwait(false);
                return (IReadOnlyList<T>)created.ToList().AsReadOnly();
            },
            ttl,
            ct).ConfigureAwait(false);
    }

    public void Invalidate(string key)
    {
        _entries.TryRemove(key, out _);
    }

    private bool TryGetValidEntry<T>(string key, out T value)
    {
        if (_entries.TryGetValue(key, out var entry)
            && entry.ExpiresAtUtc > DateTimeOffset.UtcNow
            && entry.Value is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }
}
