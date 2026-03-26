namespace StoreAssistantPro.Core.Services;

public static class AppLaunchActivationStore
{
    private static readonly Lock SyncRoot = new();
    private static AppLaunchActivationRequest? _pendingRequest;

    public static void Initialize(IEnumerable<string>? args)
    {
        lock (SyncRoot)
        {
            _pendingRequest = WindowsNotificationLaunchArguments.Parse(args);
        }
    }

    public static bool TryConsumeOpenNotificationsRequest()
    {
        return TryConsumeRequest()?.OpenNotificationsRequested == true;
    }

    public static AppLaunchActivationRequest? TryConsumeRequest()
    {
        lock (SyncRoot)
        {
            var pendingRequest = _pendingRequest;
            _pendingRequest = null;
            return pendingRequest;
        }
    }

    internal static void ResetForTests()
    {
        lock (SyncRoot)
        {
            _pendingRequest = null;
        }
    }
}
