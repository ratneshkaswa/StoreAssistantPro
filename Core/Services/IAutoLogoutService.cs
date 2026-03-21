namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Monitors user inactivity and triggers auto-logout (#462).
/// </summary>
public interface IAutoLogoutService
{
    /// <summary>Current timeout in minutes. 0 = disabled.</summary>
    int TimeoutMinutes { get; }

    /// <summary>Update the inactivity timeout. 0 disables auto-logout.</summary>
    void Configure(int timeoutMinutes);

    /// <summary>Reset the inactivity timer (call on any user interaction).</summary>
    void ResetTimer();

    /// <summary>Start monitoring inactivity.</summary>
    void Start();

    /// <summary>Stop monitoring (e.g., on manual logout).</summary>
    void Stop();

    /// <summary>Raised when the inactivity timeout expires.</summary>
    event EventHandler? InactivityTimeout;
}
