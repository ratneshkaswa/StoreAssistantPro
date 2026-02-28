namespace StoreAssistantPro.Models;

/// <summary>
/// Identifies visual zones in the main shell whose emphasis
/// is managed by <see cref="Core.Services.ICalmUIService"/>.
/// </summary>
public enum WorkspaceZone
{
    /// <summary>Top menu bar.</summary>
    MenuBar,

    /// <summary>Quick action toolbar.</summary>
    Toolbar,

    /// <summary>Primary content area (page views, billing panel).</summary>
    Content,

    /// <summary>Bottom status bar.</summary>
    StatusBar
}
