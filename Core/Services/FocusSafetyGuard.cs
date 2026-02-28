using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton implementation of <see cref="IFocusSafetyGuard"/>.
/// <para>
/// Evaluates three independent safety rules before any
/// <see cref="FocusHint"/> is executed:
/// </para>
/// <list type="number">
///   <item><b>Typing guard</b> — reads
///         <see cref="IPredictiveFocusService.IsUserInputActive"/>.
///         If the user is typing, all hints except <c>Preserve</c>
///         are blocked.</item>
///   <item><b>Dialog guard</b> — tracks modal dialog open/close via
///         <see cref="SignalDialogOpened"/>/<see cref="SignalDialogClosed"/>.
///         While a dialog is open, all hints are blocked (they would
///         target elements behind the modal and execute stale moves
///         when the dialog closes).</item>
///   <item><b>Click guard</b> — after <see cref="SignalUserClick"/>,
///         a 400 ms cooldown suppresses programmatic hints. This
///         respects the user's deliberate mouse-click target selection
///         and prevents the next queued hint from ripping focus away.</item>
/// </list>
///
/// <para><b>Thread safety:</b> All state is accessed from the UI
/// thread only (singleton, called from behaviors). No locking needed.</para>
/// </summary>
public sealed class FocusSafetyGuard : IFocusSafetyGuard
{
    private readonly IPredictiveFocusService _focusService;
    private readonly System.Timers.Timer _clickCooldownTimer;

    /// <summary>Click cooldown duration (ms).</summary>
    public const int ClickCooldownMs = 400;

    private int _dialogDepth;
    private bool _clickCooldownActive;

    public FocusSafetyGuard(IPredictiveFocusService focusService)
    {
        _focusService = focusService;

        _clickCooldownTimer = new System.Timers.Timer(ClickCooldownMs) { AutoReset = false };
        _clickCooldownTimer.Elapsed += OnClickCooldownElapsed;
    }

    // ── Public state ─────────────────────────────────────────────────

    public bool IsDialogOpen => _dialogDepth > 0;

    public bool IsClickCooldownActive => _clickCooldownActive;

    // ── Guard evaluation ─────────────────────────────────────────────

    /// <inheritdoc/>
    public bool CanExecute(FocusHint hint)
    {
        ArgumentNullException.ThrowIfNull(hint);

        // Preserve hints always pass — they are intentional no-ops
        if (hint.Strategy == FocusStrategy.Preserve)
            return true;

        // Rule 1: Typing guard
        if (_focusService.IsUserInputActive)
            return false;

        // Rule 2: Dialog guard
        if (IsDialogOpen)
            return false;

        // Rule 3: Click guard
        if (_clickCooldownActive)
            return false;

        return true;
    }

    // ── Dialog signals ───────────────────────────────────────────────

    /// <inheritdoc/>
    public void SignalDialogOpened()
    {
        _dialogDepth++;
    }

    /// <inheritdoc/>
    public void SignalDialogClosed()
    {
        if (_dialogDepth > 0)
            _dialogDepth--;
    }

    // ── Click signals ────────────────────────────────────────────────

    /// <inheritdoc/>
    public void SignalUserClick()
    {
        _clickCooldownActive = true;
        _clickCooldownTimer.Stop();
        _clickCooldownTimer.Start();
    }

    private void OnClickCooldownElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _clickCooldownActive = false;
    }
}
