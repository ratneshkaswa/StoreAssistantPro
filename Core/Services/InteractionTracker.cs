using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Lock-free singleton implementation of <see cref="IInteractionTracker"/>.
///
/// <para><b>Ring buffer design:</b></para>
/// <para>
/// Each signal type maintains a fixed-size ring buffer of
/// <c>Environment.TickCount64</c> timestamps. The <c>Record*</c> methods
/// write the next slot atomically via <see cref="Interlocked.Increment(ref long)"/>
/// on an index counter, then store the tick. The <see cref="Tick"/> callback
/// counts how many timestamps fall within the sliding window to compute
/// frequency.
/// </para>
///
/// <para><b>Memory overhead:</b>
/// 3 buffers × 128 slots × 8 bytes = 3 KB total. Fixed at startup,
/// never resized, never GC'd.</para>
/// </summary>
public sealed partial class InteractionTracker : ObservableObject, IInteractionTracker
{
    private readonly IRegionalSettingsService _regional;
    private readonly IEventBus _eventBus;
    private readonly ILogger<InteractionTracker> _logger;
    private readonly Timer _timer;
    private readonly Lock _tickLock = new();

    // ── Configuration ────────────────────────────────────────────────

    /// <summary>Timer tick interval (ms). Controls snapshot update cadence.</summary>
    internal const int TickIntervalMs = 200;

    /// <summary>Sliding window size for frequency calculation (seconds).</summary>
    internal const double WindowSeconds = 5.0;

    /// <summary>After this many idle seconds, the timer self-disables.</summary>
    internal const double MaxIdleSeconds = 30.0;

    /// <summary>Ring buffer capacity per signal type.</summary>
    private const int BufferSize = 128;

    private const int BufferMask = BufferSize - 1; // fast modulo for power-of-2

    // ── Ring buffers (fixed-size, pre-allocated) ─────────────────────

    private readonly long[] _keyTicks = new long[BufferSize];
    private readonly long[] _mouseTicks = new long[BufferSize];
    private readonly long[] _billingTicks = new long[BufferSize];

    // ── Atomic write indices ─────────────────────────────────────────
    // Monotonically increasing. Slot = index & BufferMask.

    private long _keyIndex;
    private long _mouseIndex;
    private long _billingIndex;

    // ── Last-activity timestamp (for idle calculation) ───────────────

    private long _lastActivityTick;

    // ── Timer self-disable flag ──────────────────────────────────────

    private volatile bool _timerActive;
    private bool _disposed;

    public InteractionTracker(
        IRegionalSettingsService regional,
        IEventBus eventBus,
        ILogger<InteractionTracker> logger)
    {
        _regional = regional;
        _eventBus = eventBus;
        _logger = logger;

        _lastActivityTick = Environment.TickCount64;
        CurrentSnapshot = InteractionSnapshot.Idle(_regional.Now);

        // Start with timer active
        _timerActive = true;
        _timer = new Timer(OnTimerTick, null, TickIntervalMs, TickIntervalMs);

        _logger.LogDebug("InteractionTracker initialized.");
    }

    // ── Observable properties ────────────────────────────────────────

    [ObservableProperty]
    public partial InteractionSnapshot CurrentSnapshot { get; private set; }

    // ── Record methods (hot path — lock-free, zero-allocation) ───────

    public void RecordKeyPress()
    {
        var tick = Environment.TickCount64;
        var idx = Interlocked.Increment(ref _keyIndex) & BufferMask;
        _keyTicks[idx] = tick;
        Volatile.Write(ref _lastActivityTick, tick);
        EnsureTimerActive();
    }

    public void RecordMouseMove()
    {
        var tick = Environment.TickCount64;
        var idx = Interlocked.Increment(ref _mouseIndex) & BufferMask;
        _mouseTicks[idx] = tick;
        Volatile.Write(ref _lastActivityTick, tick);
        EnsureTimerActive();
    }

    public void RecordBillingAction()
    {
        var tick = Environment.TickCount64;
        var idx = Interlocked.Increment(ref _billingIndex) & BufferMask;
        _billingTicks[idx] = tick;
        Volatile.Write(ref _lastActivityTick, tick);
        EnsureTimerActive();
    }

    // ── Tick (timer callback or manual) ──────────────────────────────

    public void Tick()
    {
        lock (_tickLock)
        {
            var now = Environment.TickCount64;
            var windowMs = (long)(WindowSeconds * 1000);

            var keyFreq = CountInWindow(_keyTicks, now, windowMs) / WindowSeconds;
            var mouseFreq = CountInWindow(_mouseTicks, now, windowMs) / WindowSeconds;

            var billingCount = CountInWindow(_billingTicks, now, windowMs);
            var billingPerMin = billingCount / WindowSeconds * 60.0;

            var idleMs = now - Volatile.Read(ref _lastActivityTick);
            var idleSec = Math.Min(idleMs / 1000.0, MaxIdleSeconds);

            var snapshot = new InteractionSnapshot(
                Math.Round(keyFreq, 2),
                Math.Round(mouseFreq, 2),
                Math.Round(idleSec, 1),
                Math.Round(billingPerMin, 1),
                _regional.Now);

            var previous = CurrentSnapshot;

            // Only publish when metrics cross significance thresholds
            if (!IsSignificantChange(previous, snapshot))
                return;

            CurrentSnapshot = snapshot;

            _ = _eventBus.PublishAsync(new InteractionSnapshotChangedEvent(snapshot));

            // Auto-disable timer during extended idle
            if (idleSec >= MaxIdleSeconds && _timerActive)
            {
                _timerActive = false;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _logger.LogDebug("InteractionTracker: timer self-disabled (idle {Idle}s).", idleSec);
            }
        }
    }

    // ── Dispose ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _timer.Dispose();
    }

    // ── Internal helpers ─────────────────────────────────────────────

    /// <summary>
    /// Counts how many entries in the ring buffer fall within
    /// <paramref name="windowMs"/> of <paramref name="now"/>.
    /// O(BufferSize) — 128 iterations, branch-free body.
    /// </summary>
    private static int CountInWindow(long[] buffer, long now, long windowMs)
    {
        var cutoff = now - windowMs;
        var count = 0;

        for (var i = 0; i < BufferSize; i++)
        {
            var tick = Volatile.Read(ref buffer[i]);
            if (tick > cutoff && tick <= now)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Returns <c>true</c> when the new snapshot is materially
    /// different from the previous one — avoids flooding events
    /// with micro-changes.
    /// </summary>
    private static bool IsSignificantChange(
        InteractionSnapshot previous, InteractionSnapshot current)
    {
        // Rapid-input flag changed
        if (previous.IsRapidInput != current.IsRapidInput)
            return true;

        // Idle flag changed
        if (previous.IsIdle != current.IsIdle)
            return true;

        // Keyboard frequency crossed a whole-number boundary
        if ((int)previous.KeyboardFrequency != (int)current.KeyboardFrequency)
            return true;

        // Mouse frequency crossed a whole-number boundary
        if ((int)previous.MouseFrequency != (int)current.MouseFrequency)
            return true;

        // Billing rate crossed a 10-action boundary
        if ((int)(previous.BillingActionsPerMinute / 10) !=
            (int)(current.BillingActionsPerMinute / 10))
            return true;

        // Idle seconds crossed a 1-second boundary
        if ((int)previous.IdleSeconds != (int)current.IdleSeconds)
            return true;

        return false;
    }

    /// <summary>
    /// Re-enables the timer if it was self-disabled during idle.
    /// Called from the <c>Record*</c> methods.
    /// </summary>
    private void EnsureTimerActive()
    {
        if (_timerActive) return;
        _timerActive = true;
        try
        {
            _timer.Change(TickIntervalMs, TickIntervalMs);
        }
        catch (ObjectDisposedException)
        {
            // Timer already disposed during shutdown — safe to ignore
        }
    }

    /// <summary>Timer callback — delegates to <see cref="Tick"/>.</summary>
    private void OnTimerTick(object? state) => Tick();
}
