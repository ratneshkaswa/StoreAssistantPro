namespace StoreAssistantPro.Modules.MainShell.Models;

public sealed class CommandPaletteItem
{
    public CommandPaletteItem(
        string id,
        string title,
        string description,
        string icon,
        string shortcutText,
        string category,
        int sortOrder,
        bool isRecent,
        QuickAction action)
    {
        Id = id;
        Title = title;
        Description = description;
        Icon = icon;
        ShortcutText = shortcutText;
        Category = category;
        SortOrder = sortOrder;
        IsRecent = isRecent;
        Action = action;
    }

    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public string Icon { get; }
    public string ShortcutText { get; }
    public string Category { get; }
    public int SortOrder { get; }
    public bool IsRecent { get; }
    public QuickAction Action { get; }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasShortcutText => !string.IsNullOrWhiteSpace(ShortcutText);
}
