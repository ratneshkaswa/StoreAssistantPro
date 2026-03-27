namespace StoreAssistantPro.Core.Services;

public interface IReferenceDataCache
{
    Task<T> GetOrCreateValueAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<IReadOnlyList<T>>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);

    void Invalidate(string key);
}
