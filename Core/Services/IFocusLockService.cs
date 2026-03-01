using System.ComponentModel;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Tracks whether UI focus is locked to a specific module.
/// <para>
/// When the application enters <see cref="Models.OperationalMode.Billing"/>,
/// focus locks to the billing module — navigation away from the POS
/// terminal is restricted to prevent accidental context switches
/// during active sales.
/// </para>
/// <para>
/// <b>Release rules:</b>
/// <list type="bullet">
///   <item>Lock releases when the operational mode returns to Management
///         (bill completed, bill cancelled, or billing session ends).</item>
///   <item>Release is <b>held</b> while <see cref="IsReleaseHeld"/> is
///         <c>true</c> (e.g., payment is processing). The pending release
///         fires automatically when the hold is lifted.</item>
/// </list>
/// </para>
/// <para>
/// <b>Architecture rule:</b> ViewModels read <see cref="IsFocusLocked"/>
/// and <see cref="ActiveModule"/> to gate navigation commands.
/// Only the billing mode service
/// and this service react to mode changes — no UI logic lives here.
/// </para>
/// </summary>
public interface IFocusLockService : INotifyPropertyChanged
{
    /// <summary>
    /// <c>true</c> when the UI is locked to a specific module and
    /// free navigation is restricted.
    /// </summary>
    bool IsFocusLocked { get; }

    /// <summary>
    /// The module key that currently holds the focus lock, or
    /// <see cref="string.Empty"/> when no lock is active.
    /// </summary>
    string ActiveModule { get; }

    /// <summary>
    /// <c>true</c> when a release hold is active, preventing the focus
    /// lock from being released (e.g., during payment processing).
    /// </summary>
    bool IsReleaseHeld { get; }

    /// <summary>
    /// Activates the focus lock for the given module.
    /// </summary>
    /// <param name="moduleKey">Identifier of the module acquiring the lock
    /// (e.g., <c>"Billing"</c>).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a different module already holds the lock.
    /// </exception>
    void Acquire(string moduleKey);

    /// <summary>
    /// Releases the focus lock. No-op if the lock is not active.
    /// If <see cref="IsReleaseHeld"/> is <c>true</c>, the release is
    /// deferred until <see cref="LiftReleaseHold"/> is called.
    /// </summary>
    /// <param name="moduleKey">Must match the module that acquired the lock.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="moduleKey"/> does not match the
    /// current lock holder.
    /// </exception>
    void Release(string moduleKey);

    /// <summary>
    /// Activates a release hold. While held, calls to <see cref="Release"/>
    /// are deferred instead of executed immediately.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a hold is already active.
    /// </exception>
    void HoldRelease();

    /// <summary>
    /// Lifts the release hold. If a release was deferred, it executes now.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no hold is active.
    /// </exception>
    void LiftReleaseHold();
}
