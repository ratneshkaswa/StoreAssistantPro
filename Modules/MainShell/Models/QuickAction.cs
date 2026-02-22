using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.MainShell.Models;

/// <summary>
/// Represents a single action in a POS quick-action toolbar.
/// <para>
/// Designed for dynamic toolbar generation via MVVM — bind an
/// <c>ObservableCollection&lt;QuickAction&gt;</c> to an <c>ItemsControl</c>
/// and use <see cref="Command"/> as the button's <c>Command</c>.
/// </para>
/// <para><b>Usage:</b></para>
/// <code>
/// QuickActions =
/// [
///     new() { Title = "New Sale",  Icon = "🛒", Command = NewSaleCommand,  ShortcutText = "F5", SortOrder = 0 },
///     new() { Title = "Products",  Icon = "📦", Command = ProductsCommand, ShortcutText = "F6", SortOrder = 1 },
///     new() { Title = "Settings",  Icon = "⚙️", Command = SettingsCommand, ShortcutText = "F9", SortOrder = 9, IsVisible = false },
/// ];
/// </code>
/// </summary>
public partial class QuickAction : ObservableObject
{
    /// <summary>Display label shown on the toolbar button.</summary>
    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>Emoji or icon-font glyph displayed alongside the title.</summary>
    [ObservableProperty]
    public partial string Icon { get; set; }

    /// <summary>Command executed when the action is triggered.</summary>
    [ObservableProperty]
    public partial ICommand? Command { get; set; }

    /// <summary>Keyboard shortcut hint displayed on the button (e.g. "F5").</summary>
    [ObservableProperty]
    public partial string ShortcutText { get; set; }

    /// <summary>
    /// Parseable key gesture string (e.g. "Ctrl+D", "F5").
    /// When set, <see cref="IShortcutService"/> creates a <c>KeyBinding</c>
    /// that executes <see cref="Command"/>. Leave <c>null</c> for no shortcut.
    /// </summary>
    public string? Gesture { get; init; }

    /// <summary>Controls toolbar-button visibility at runtime.</summary>
    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    /// <summary>Determines display order in the toolbar (ascending).</summary>
    [ObservableProperty]
    public partial int SortOrder { get; set; }

    /// <summary>
    /// Roles that may see this action. Empty means all roles.
    /// Used by <c>IQuickActionService.GetVisibleActions</c> for role filtering.
    /// </summary>
    public IReadOnlyList<UserType> RequiredRoles { get; init; } = [];

    public QuickAction()
    {
        Title = string.Empty;
        Icon = string.Empty;
        ShortcutText = string.Empty;
        IsVisible = true;
    }
}
