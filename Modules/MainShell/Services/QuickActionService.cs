using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Models;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Thread-safe singleton registry for POS quick-action toolbar items.
/// <para>
/// Modules register their actions during DI setup or ViewModel
/// construction.  The shell reads the sorted list to build the toolbar.
/// </para>
/// </summary>
public class QuickActionService : IQuickActionService
{
    private readonly List<QuickAction> _actions = [];
    private readonly object _lock = new();

    public void Register(QuickAction action)
    {
        lock (_lock)
        {
            if (_actions.Any(a => string.Equals(a.Title, action.Title, StringComparison.OrdinalIgnoreCase)))
                return;

            _actions.Add(action);
        }
    }

    public IReadOnlyList<QuickAction> GetActions()
    {
        lock (_lock)
        {
            return _actions
                .OrderBy(a => a.SortOrder)
                .ToList()
                .AsReadOnly();
        }
    }

    public IReadOnlyList<QuickAction> GetVisibleActions(UserType role, IFeatureToggleService features)
    {
        lock (_lock)
        {
            return _actions
                .Where(a => a.IsVisible)
                .Where(a => a.RequiredRoles.Count == 0 || a.RequiredRoles.Contains(role))
                .Where(a => a.RequiredFeature is null || features.IsEnabled(a.RequiredFeature))
                .OrderBy(a => a.SortOrder)
                .ToList()
                .AsReadOnly();
        }
    }

    public void SetVisibility(string title, bool isVisible)
    {
        lock (_lock)
        {
            var action = _actions.FirstOrDefault(
                a => string.Equals(a.Title, title, StringComparison.OrdinalIgnoreCase));

            if (action is not null)
                action.IsVisible = isVisible;
        }
    }
}
