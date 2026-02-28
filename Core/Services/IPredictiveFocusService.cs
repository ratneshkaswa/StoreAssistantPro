using System.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton decision engine that produces intelligent focus hints
/// based on workflow transitions.
/// <para>
/// <b>Responsibilities:</b>
/// <list type="bullet">
///   <item>Track the active page and operational context.</item>
///   <item>React to navigation, mode changes, and focus lock transitions.</item>
///   <item>Emit <see cref="FocusHint"/> recommendations — never touch the
///         visual tree or call <c>UIElement.Focus()</c>.</item>
///   <item>Publish <see cref="Events.FocusHintChangedEvent"/> so XAML
///         behaviors can execute the actual focus move.</item>
/// </list>
/// </para>
///
/// <para><b>Hint production rules:</b></para>
/// <code>
///   ┌───────────────────────────┬──────────────────────────────────┐
///   │ Transition                │ Hint                             │
///   ├───────────────────────────┼──────────────────────────────────┤
///   │ Page navigation           │ FirstInput (content area)        │
///   │ Billing lock acquired     │ Named("BillingSearchBox")        │
///   │ Billing lock released     │ FirstInput (restored page)       │
///   │ Form opened (BottomForm)  │ FirstInput (form area)           │
///   │ Dialog opened             │ FirstInput (dialog)              │
///   │ User actively typing      │ Preserve                         │
///   └───────────────────────────┴──────────────────────────────────┘
/// </code>
///
/// <para><b>Architecture rules:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>Depends only on other singleton services and IEventBus.</item>
///   <item>No WPF types — pure logic service.</item>
///   <item>ViewModels read <see cref="CurrentHint"/> or subscribe to
///         the event for reactive updates.</item>
/// </list>
/// </summary>
public interface IPredictiveFocusService : INotifyPropertyChanged
{
    /// <summary>
    /// The most recently produced focus hint, or <c>null</c> if no
    /// transition has occurred yet.
    /// </summary>
    FocusHint? CurrentHint { get; }

    /// <summary>
    /// <c>true</c> when the service detects the user is actively
    /// interacting with an input (typing, editing). Focus transitions
    /// are suppressed while active to avoid disrupting the user.
    /// </summary>
    bool IsUserInputActive { get; }

    /// <summary>
    /// Notify the service that the user is actively typing or editing.
    /// Resets automatically after the idle timeout.
    /// Call from XAML behaviors that detect keystrokes.
    /// </summary>
    void SignalUserInput();

    /// <summary>
    /// Request a focus hint for a specific element by name.
    /// Useful when a ViewModel knows exactly where focus should go
    /// (e.g., after a save operation returns the cursor to the form).
    /// </summary>
    /// <param name="elementName">The <c>x:Name</c> of the target element.</param>
    /// <param name="reason">Diagnostic label for the hint.</param>
    void RequestFocus(string elementName, string reason);

    /// <summary>
    /// Request a focus hint for the first focusable input in the
    /// active content area.
    /// </summary>
    /// <param name="reason">Diagnostic label for the hint.</param>
    void RequestFirstInput(string reason);
}
