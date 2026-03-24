namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Time-limited undo buffer for the current session (#439).
/// Holds at most one undo action with a configurable TTL.
/// </summary>
public class UndoService : IUndoService
{
    private UndoEntry? _pending;
    private readonly object _lock = new();

    public bool HasPending
    {
        get
        {
            lock (_lock)
            {
                CleanExpired();
                return _pending is not null;
            }
        }
    }

    public string? PendingDescription
    {
        get
        {
            lock (_lock)
            {
                CleanExpired();
                return _pending?.Description;
            }
        }
    }

    public void Register(string key, string description, Func<CancellationToken, Task> undoAction, int ttlSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(undoAction);

        lock (_lock)
        {
            _pending = new UndoEntry(key, description, undoAction, DateTime.UtcNow.AddSeconds(ttlSeconds));
        }
    }

    public async Task<UndoResult> UndoLastAsync(CancellationToken ct = default)
    {
        UndoEntry? entry;
        lock (_lock)
        {
            CleanExpired();
            entry = _pending;
            _pending = null;
        }

        if (entry is null)
            return new UndoResult(false, "No undoable action available.");

        try
        {
            await entry.Action(ct).ConfigureAwait(false);
            return new UndoResult(true, $"Undone: {entry.Description}");
        }
        catch (Exception ex)
        {
            return new UndoResult(false, $"Undo failed: {ex.Message}");
        }
    }

    private void CleanExpired()
    {
        if (_pending is not null && _pending.ExpiresAt < DateTime.UtcNow)
            _pending = null;
    }

    private sealed record UndoEntry(
        string Key,
        string Description,
        Func<CancellationToken, Task> Action,
        DateTime ExpiresAt);
}
