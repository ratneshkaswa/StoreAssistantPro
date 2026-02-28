using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton service that manages UI focus locking.
/// <para>
/// Subscribes to <see cref="OperationalModeChangedEvent"/> and
/// automatically acquires/releases the focus lock when the mode
/// transitions between Management and Billing.
/// </para>
/// <para>
/// <b>Lock semantics:</b>
/// <list type="bullet">
///   <item><see cref="Acquire"/> is idempotent for the same module — calling
///         it twice with the same key is a no-op.</item>
///   <item><see cref="Release"/> is idempotent when no lock is held.</item>
///   <item>Cross-module conflicts throw <see cref="InvalidOperationException"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Release-hold (payment safety):</b>
/// <list type="bullet">
///   <item><see cref="HoldRelease"/> prevents any release from executing
///         (e.g., called when payment processing starts).</item>
///   <item>While held, <see cref="Release"/> records the intent but does
///         not unlock.</item>
///   <item><see cref="LiftReleaseHold"/> flushes any deferred release
///         (e.g., called when payment processing ends).</item>
/// </list>
/// </para>
/// </summary>
public partial class FocusLockService : ObservableObject, IFocusLockService, IDisposable
{
    private const string BillingModule = "Billing";

    private readonly IEventBus _eventBus;
    private readonly object _lock = new();

    private bool _isReleaseHeld;
    private string? _deferredReleaseModule;

    public FocusLockService(IEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<OperationalModeChangedEvent>(OnModeChangedAsync);
    }

    // ── Observable state ──

    [ObservableProperty]
    public partial bool IsFocusLocked { get; private set; }

    [ObservableProperty]
    public partial string ActiveModule { get; private set; } = string.Empty;

    public bool IsReleaseHeld
    {
        get { lock (_lock) return _isReleaseHeld; }
    }

    // ── Public API ──

    public void Acquire(string moduleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);

        lock (_lock)
        {
            // Acquiring clears any pending deferred release — the caller
            // is explicitly re-establishing the lock.
            _deferredReleaseModule = null;

            // Idempotent for the same module
            if (IsFocusLocked && ActiveModule == moduleKey)
                return;

            if (IsFocusLocked)
                throw new InvalidOperationException(
                    $"Focus lock is already held by '{ActiveModule}'. " +
                    $"Cannot acquire for '{moduleKey}'.");

            ActiveModule = moduleKey;
            IsFocusLocked = true;
        }
    }

    public void Release(string moduleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);

        lock (_lock)
        {
            if (!IsFocusLocked)
                return;

            if (ActiveModule != moduleKey)
                throw new InvalidOperationException(
                    $"Focus lock is held by '{ActiveModule}'. " +
                    $"Cannot release for '{moduleKey}'.");

            // Defer if a hold is active (e.g., payment processing)
            if (_isReleaseHeld)
            {
                _deferredReleaseModule = moduleKey;
                return;
            }

            ActiveModule = string.Empty;
            IsFocusLocked = false;
        }
    }

    // ── Release hold (payment safety) ──

    public void HoldRelease()
    {
        lock (_lock)
        {
            if (_isReleaseHeld)
                throw new InvalidOperationException(
                    "A release hold is already active.");

            _isReleaseHeld = true;
            _deferredReleaseModule = null;
        }
    }

    public void LiftReleaseHold()
    {
        string? deferred;

        lock (_lock)
        {
            if (!_isReleaseHeld)
                throw new InvalidOperationException(
                    "No release hold is currently active.");

            _isReleaseHeld = false;
            deferred = _deferredReleaseModule;
            _deferredReleaseModule = null;
        }

        // Flush deferred release outside the lock to avoid re-entrancy
        if (deferred is not null)
            Release(deferred);
    }

    // ── Auto mode switching ──

    private Task OnModeChangedAsync(OperationalModeChangedEvent e)
    {
        if (e.NewMode == OperationalMode.Billing)
            Acquire(BillingModule);
        else
            Release(BillingModule);

        return Task.CompletedTask;
    }

    // ── Cleanup ──

    public void Dispose()
    {
        _eventBus.Unsubscribe<OperationalModeChangedEvent>(OnModeChangedAsync);
    }
}
