namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Provides time-limited undo for destructive operations within the current session (#439).
/// </summary>
public interface IUndoService
{
    /// <summary>Register a reversible action. Expires after <paramref name="ttlSeconds"/>.</summary>
    void Register(string key, string description, Func<CancellationToken, Task> undoAction, int ttlSeconds = 30);

    /// <summary>Undo the most recent registered action, if it hasn't expired.</summary>
    Task<UndoResult> UndoLastAsync(CancellationToken ct = default);

    /// <summary>Check whether there is a pending undoable action.</summary>
    bool HasPending { get; }

    /// <summary>Description of the pending undoable action, if any.</summary>
    string? PendingDescription { get; }
}

public record UndoResult(bool Success, string Message);
