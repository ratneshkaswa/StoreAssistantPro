using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Pure-logic guard that determines whether a <see cref="FocusHint"/>
/// is safe to execute at the current moment.
/// <para>
/// <b>Safety rules (all must pass):</b>
/// </para>
/// <list type="number">
///   <item><b>Typing guard</b> — never steal focus while the user is
///         actively typing (<see cref="IPredictiveFocusService.IsUserInputActive"/>).</item>
///   <item><b>Dialog guard</b> — suppress hints while a modal dialog
///         is open (hints would queue stale targets).</item>
///   <item><b>Click guard</b> — respect manual mouse clicks; suppress
///         programmatic focus changes for a brief cooldown after a
///         user-initiated focus move.</item>
/// </list>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>No WPF types — pure logic, fully unit-testable.</item>
///   <item>Called by <c>PredictiveFocusBehavior</c> before executing
///         any hint. The behavior is the only WPF-aware consumer.</item>
///   <item>Coordinates with <see cref="IPredictiveFocusService"/> for
///         the typing guard (reads <c>IsUserInputActive</c>).</item>
/// </list>
/// </summary>
public interface IFocusSafetyGuard
{
    /// <summary>
    /// Returns <c>true</c> if the given <paramref name="hint"/> is safe
    /// to execute right now. Returns <c>false</c> if any safety rule
    /// blocks it.
    /// </summary>
    bool CanExecute(FocusHint hint);

    /// <summary>
    /// Signal that a modal dialog has been opened. Hints are suppressed
    /// until <see cref="SignalDialogClosed"/> is called.
    /// </summary>
    void SignalDialogOpened();

    /// <summary>
    /// Signal that a modal dialog has been closed. Hint suppression
    /// from the dialog guard is lifted.
    /// </summary>
    void SignalDialogClosed();

    /// <summary>
    /// Signal that the user performed a deliberate mouse click on an
    /// input control. Programmatic focus hints are suppressed for a
    /// brief cooldown (<see cref="FocusSafetyGuard.ClickCooldownMs"/>)
    /// to respect the user's manual target selection.
    /// </summary>
    void SignalUserClick();

    /// <summary>
    /// <c>true</c> when a modal dialog is currently open.
    /// </summary>
    bool IsDialogOpen { get; }

    /// <summary>
    /// <c>true</c> when the click cooldown is active (user recently
    /// clicked an input control).
    /// </summary>
    bool IsClickCooldownActive { get; }
}
