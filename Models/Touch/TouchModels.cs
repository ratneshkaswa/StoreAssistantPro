namespace StoreAssistantPro.Models.Touch;

/// <summary>Touch/kiosk configuration (#788-798).</summary>
public sealed class TouchConfig
{
    public int MinTouchTargetPx { get; set; } = 44;
    public bool IsKioskMode { get; set; }
    public bool ShowOnScreenNumpad { get; set; }
    public bool EnableSwipeGestures { get; set; }
    public bool EnablePinchZoom { get; set; }
    public bool AutoHideTaskbar { get; set; }
    public bool EnableTouchKeyboard { get; set; }
    public int DataGridRowHeight { get; set; } = 48;
}

/// <summary>Gesture shortcut mapping (#796).</summary>
public sealed record GestureShortcut(
    string GestureName,
    string ActionName,
    string? Description);
