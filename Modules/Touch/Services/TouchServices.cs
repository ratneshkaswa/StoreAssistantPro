using Microsoft.Extensions.Logging;
using StoreAssistantPro.Models.Touch;

namespace StoreAssistantPro.Modules.Touch.Services;

public sealed class TouchModeService(ILogger<TouchModeService> logger) : ITouchModeService
{
    private TouchConfig _config = new();

    public TouchConfig GetConfig() => _config;
    public bool IsKioskMode => _config.IsKioskMode;

    public Task SaveConfigAsync(TouchConfig config, CancellationToken ct = default)
    {
        _config = config;
        logger.LogInformation("Touch config saved. Kiosk={Kiosk}", config.IsKioskMode);
        return Task.CompletedTask;
    }

    public Task EnterKioskModeAsync(CancellationToken ct = default)
    {
        _config.IsKioskMode = true;
        logger.LogInformation("Entered kiosk mode");
        return Task.CompletedTask;
    }

    public Task ExitKioskModeAsync(CancellationToken ct = default)
    {
        _config.IsKioskMode = false;
        logger.LogInformation("Exited kiosk mode");
        return Task.CompletedTask;
    }
}

public sealed class GestureService(ILogger<GestureService> logger) : IGestureService
{
    private List<GestureShortcut> _shortcuts =
    [
        new("SwipeLeft", "DeleteCartItem", "Swipe left to remove item from cart"),
        new("SwipeRight", "NavigateBack", "Swipe right to go back"),
        new("DoubleTap", "QuickAdd", "Double-tap to add item to cart"),
        new("LongPress", "ShowDetails", "Long press for item details")
    ];

    public IReadOnlyList<GestureShortcut> GetShortcuts() => _shortcuts;

    public Task SaveShortcutsAsync(IReadOnlyList<GestureShortcut> shortcuts, CancellationToken ct = default)
    {
        _shortcuts = [.. shortcuts];
        logger.LogInformation("Saved {Count} gesture shortcuts", shortcuts.Count);
        return Task.CompletedTask;
    }
}

public sealed class OnScreenInputService : IOnScreenInputService
{
    public bool IsNumpadVisible { get; private set; }
    public bool IsKeyboardVisible { get; private set; }
    public void ShowNumpad() => IsNumpadVisible = true;
    public void HideNumpad() => IsNumpadVisible = false;
    public void ShowKeyboard() => IsKeyboardVisible = true;
    public void HideKeyboard() => IsKeyboardVisible = false;
}
