namespace StoreAssistantPro.Models;

/// <summary>
/// Determines how a <see cref="FocusHint"/> resolves to a visual-tree element.
/// </summary>
public enum FocusStrategy
{
    /// <summary>
    /// Move focus to the first focusable input inside the active content area.
    /// Resolved via <c>FocusNavigationDirection.First</c>.
    /// </summary>
    FirstInput,

    /// <summary>
    /// Move focus to a specific named element (by <c>x:Name</c> or
    /// <c>AutomationProperties.AutomationId</c>).
    /// </summary>
    Named,

    /// <summary>
    /// Do not move focus — keep it wherever it currently is.
    /// Useful when the user is mid-typing in a search box or form.
    /// </summary>
    Preserve
}
