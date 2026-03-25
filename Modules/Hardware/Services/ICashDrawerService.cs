using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Manages electronic cash drawers.
/// Features #487–494: auto-open, manual open, status, multiple drawers,
/// assignment, open count, close alert, USB/serial support.
/// </summary>
public interface ICashDrawerService
{
    /// <summary>Current state of the primary drawer.</summary>
    CashDrawerState CurrentState { get; }

    /// <summary>Open the cash drawer automatically on sale complete. (#487)</summary>
    Task<bool> AutoOpenAsync(CancellationToken ct = default);

    /// <summary>Manually open the drawer (requires manager PIN). (#488)</summary>
    Task<bool> ManualOpenAsync(CancellationToken ct = default);

    /// <summary>Detect open/closed state. (#489)</summary>
    Task<bool> IsDrawerOpenAsync(CancellationToken ct = default);

    /// <summary>Get all connected drawers. (#490)</summary>
    Task<IReadOnlyList<CashDrawerState>> GetAllDrawersAsync(CancellationToken ct = default);

    /// <summary>Assign a drawer to a specific register/workstation. (#491)</summary>
    Task AssignToRegisterAsync(string drawerName, string registerName, CancellationToken ct = default);

    /// <summary>Get count of drawer opens this shift. (#492)</summary>
    int GetOpenCountThisShift();

    /// <summary>Reset the open count (called at shift start).</summary>
    void ResetOpenCount();

    /// <summary>Set alert timeout in seconds for drawer left open. (#493)</summary>
    int DrawerOpenAlertSeconds { get; set; }

    /// <summary>Configure a drawer device (USB or serial). (#494)</summary>
    Task<bool> ConfigureAsync(HardwareDeviceConfig config, CancellationToken ct = default);
}
