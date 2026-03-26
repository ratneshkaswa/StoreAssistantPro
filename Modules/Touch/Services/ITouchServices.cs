using StoreAssistantPro.Models.Touch;

namespace StoreAssistantPro.Modules.Touch.Services;

/// <summary>Touch and kiosk mode service (#788-798).</summary>
public interface ITouchModeService
{
    TouchConfig GetConfig();
    Task SaveConfigAsync(TouchConfig config, CancellationToken ct = default);
    Task EnterKioskModeAsync(CancellationToken ct = default);
    Task ExitKioskModeAsync(CancellationToken ct = default);
    bool IsKioskMode { get; }
}

/// <summary>Gesture shortcut management (#796).</summary>
public interface IGestureService
{
    IReadOnlyList<GestureShortcut> GetShortcuts();
    Task SaveShortcutsAsync(IReadOnlyList<GestureShortcut> shortcuts, CancellationToken ct = default);
}

/// <summary>On-screen input service (#789, #795).</summary>
public interface IOnScreenInputService
{
    void ShowNumpad();
    void HideNumpad();
    void ShowKeyboard();
    void HideKeyboard();
    bool IsNumpadVisible { get; }
    bool IsKeyboardVisible { get; }
}
