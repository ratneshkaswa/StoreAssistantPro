using System.Windows;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Syncs keyboard shortcuts from registered <see cref="Models.QuickAction"/>
/// items into a <see cref="Window"/>'s <see cref="UIElement.InputBindings"/>.
/// <para>
/// Called by the shell after <see cref="IQuickActionService"/> populates the
/// toolbar, so every quick-action shortcut "just works" without hardcoding
/// <c>KeyBinding</c> entries in XAML.
/// </para>
/// </summary>
public interface IShortcutService
{
    /// <summary>
    /// Replaces the <paramref name="window"/>'s input bindings with
    /// <c>KeyBinding</c>s derived from all registered quick actions
    /// that have a non-null <see cref="Models.QuickAction.Gesture"/>.
    /// </summary>
    void Apply(Window window);
}
