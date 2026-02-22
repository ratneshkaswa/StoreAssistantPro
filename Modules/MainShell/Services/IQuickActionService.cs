using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Models;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Central registry for POS quick-action toolbar items.
/// <para>
/// Modules call <see cref="Register"/> at startup to contribute actions.
/// The shell ViewModel calls <see cref="GetActions"/> or
/// <see cref="GetVisibleActions"/> to populate the toolbar.
/// </para>
/// <para>
/// Registered as <b>singleton</b> so all modules share one registry.
/// </para>
/// </summary>
public interface IQuickActionService
{
    /// <summary>Adds an action to the registry.</summary>
    void Register(QuickAction action);

    /// <summary>Returns all registered actions ordered by <see cref="QuickAction.SortOrder"/>.</summary>
    IReadOnlyList<QuickAction> GetActions();

    /// <summary>
    /// Returns actions that are visible and permitted for <paramref name="role"/>,
    /// ordered by <see cref="QuickAction.SortOrder"/>.
    /// Actions with an empty <see cref="QuickAction.RequiredRoles"/> list are
    /// available to all roles.
    /// </summary>
    IReadOnlyList<QuickAction> GetVisibleActions(UserType role);

    /// <summary>
    /// Sets <see cref="QuickAction.IsVisible"/> for the action matching
    /// <paramref name="title"/>. Use for feature toggles at runtime.
    /// </summary>
    void SetVisibility(string title, bool isVisible);
}
