namespace StoreAssistantPro.Models;

/// <summary>
/// Identifies the type of UI context that a focus rule is evaluated against.
/// </summary>
public enum FocusContextType
{
    /// <summary>A content page displayed in the main workspace.</summary>
    Page,

    /// <summary>A modal dialog window.</summary>
    Dialog,

    /// <summary>A collapsible bottom form within a page.</summary>
    Form
}
