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
///     new() { Title = "New Sale", Icon = "\uE7BF", Command = NewSaleCommand, ShortcutText = "F5", SortOrder = 0 },
///     new() { Title = "Products", Icon = "\uE719", Command = ProductsCommand, ShortcutText = "F6", SortOrder = 1 },
///     new() { Title = "Settings", Icon = "\uE713", Command = SettingsCommand, ShortcutText = "F9", SortOrder = 9, IsVisible = false },
/// ];
/// </code>
/// </summary>
public partial class QuickAction : ObservableObject
{
    /// <summary>Display label shown on the toolbar button.</summary>
    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>Icon-font glyph displayed alongside the title.</summary>
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

    /// <summary>Whether this action points at the currently active page.</summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>Determines display order in the toolbar (ascending).</summary>
    [ObservableProperty]
    public partial int SortOrder { get; set; }

    /// <summary>
    /// Help text shown in the SmartTooltip when the user hovers the toolbar button.
    /// Rendered as the tooltip description line below the title.
    /// </summary>
    [ObservableProperty]
    public partial string Description { get; set; }

    /// <summary>
    /// Stable key used by <see cref="Core.Services.IContextHelpService"/> to
    /// resolve context-aware help text and usage tips. When <c>null</c>,
    /// context help resolution is skipped for this action.
    /// </summary>
    public string? HelpKey { get; init; }

    /// <summary>
    /// Roles that may see this action. Empty means all roles.
    /// Used by <c>IQuickActionService.GetVisibleActions</c> for role filtering.
    /// </summary>
    public IReadOnlyList<UserType> RequiredRoles { get; init; } = [];

    /// <summary>
    /// Feature flag that must be enabled for this action to appear.
    /// <c>null</c> means the action is always available regardless of mode.
    /// Checked by <c>IQuickActionService.GetVisibleActions</c> via
    /// <see cref="Core.Features.IFeatureToggleService"/>.
    /// </summary>
    public string? RequiredFeature { get; init; }

    public QuickAction()
    {
        Title = string.Empty;
        Icon = string.Empty;
        ShortcutText = string.Empty;
        Description = string.Empty;
        IsVisible = true;
    }
}
