using System.Windows;
using System.Windows.Input;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Reads all registered quick actions and creates <see cref="KeyBinding"/>
/// entries for those that define a <see cref="Models.QuickAction.Gesture"/>.
/// </summary>
public class ShortcutService(IQuickActionService quickActionService) : IShortcutService
{
    private static readonly KeyGestureConverter GestureConverter = new();

    public void Apply(Window window)
    {
        window.InputBindings.Clear();

        foreach (var action in quickActionService.GetActions())
        {
            if (action.Gesture is null || action.Command is null)
                continue;

            if (GestureConverter.ConvertFromString(action.Gesture) is KeyGesture gesture)
                window.InputBindings.Add(new KeyBinding(action.Command, gesture));
        }
    }
}
