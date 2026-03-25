using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Hardware;
using StoreAssistantPro.Modules.Hardware.Events;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Cash drawer service — sends open commands via ESC/POS kick
/// or direct USB/serial signals. Tracks open/close state and counts.
/// </summary>
public sealed class CashDrawerService(
    IEventBus eventBus,
    ILogger<CashDrawerService> logger) : ICashDrawerService
{
    private readonly CashDrawerState _primaryState = new();
    private readonly List<CashDrawerState> _allDrawers = [];
    private HardwareDeviceConfig? _config;

    public CashDrawerState CurrentState => _primaryState;
    public int DrawerOpenAlertSeconds { get; set; } = 30;

    public async Task<bool> AutoOpenAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Auto-opening cash drawer on sale complete");
        return await OpenDrawerInternalAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> ManualOpenAsync(CancellationToken ct = default)
    {
        // Caller must validate manager PIN before calling this.
        logger.LogInformation("Manual cash drawer open requested");
        return await OpenDrawerInternalAsync(ct).ConfigureAwait(false);
    }

    public Task<bool> IsDrawerOpenAsync(CancellationToken ct = default)
    {
        // Real implementation: read DLE EOT 1 status byte, bit 2 = drawer open.
        return Task.FromResult(_primaryState.IsOpen);
    }

    public Task<IReadOnlyList<CashDrawerState>> GetAllDrawersAsync(CancellationToken ct = default)
    {
        if (_allDrawers.Count == 0)
            _allDrawers.Add(_primaryState);

        return Task.FromResult<IReadOnlyList<CashDrawerState>>(_allDrawers.AsReadOnly());
    }

    public Task AssignToRegisterAsync(string drawerName, string registerName, CancellationToken ct = default)
    {
        var drawer = _allDrawers.Find(d => d.AssignedRegister == drawerName) ?? _primaryState;
        drawer.AssignedRegister = registerName;
        logger.LogInformation("Drawer assigned to register: {Register}", registerName);
        return Task.CompletedTask;
    }

    public int GetOpenCountThisShift() => _primaryState.OpenCountThisShift;

    public void ResetOpenCount()
    {
        _primaryState.OpenCountThisShift = 0;
        logger.LogDebug("Drawer open count reset for new shift");
    }

    public Task<bool> ConfigureAsync(HardwareDeviceConfig config, CancellationToken ct = default)
    {
        _config = config;
        logger.LogInformation("Cash drawer configured: {Name} via {Type}",
            config.DeviceName, config.ConnectionType);
        return Task.FromResult(true);
    }

    private async Task<bool> OpenDrawerInternalAsync(CancellationToken ct)
    {
        // Real implementation: send ESC p 0 25 120 to printer port,
        // or toggle DTR/RTS on a serial-connected drawer.
        _primaryState.IsOpen = true;
        _primaryState.OpenCountThisShift++;
        _primaryState.LastOpenedUtc = DateTime.UtcNow;

        logger.LogDebug("Cash drawer opened (count this shift: {Count})",
            _primaryState.OpenCountThisShift);

        await eventBus.PublishAsync(new CashDrawerStateChangedEvent(_primaryState)).ConfigureAwait(false);

        // Start close-detection timer in production.
        _ = MonitorDrawerCloseAsync(ct);

        return true;
    }

    private async Task MonitorDrawerCloseAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(DrawerOpenAlertSeconds), ct).ConfigureAwait(false);
            if (_primaryState.IsOpen)
            {
                logger.LogWarning("Cash drawer has been open for {Seconds}s — alert triggered",
                    DrawerOpenAlertSeconds);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }
}
