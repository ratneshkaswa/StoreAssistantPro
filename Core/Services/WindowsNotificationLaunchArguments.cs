using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

public sealed record AppLaunchActivationRequest(
    bool OpenNotificationsRequested,
    Guid? NotificationId,
    string? PageKey);

public static class WindowsNotificationLaunchArguments
{
    private const string ActionKey = "action";
    private const string NotificationIdKey = "notificationId";
    private const string PageKey = "pageKey";
    private const string OpenNotificationsAction = "open-notifications";

    public static string Build(AppNotification notification)
    {
        var parts = new List<string>
        {
            $"{ActionKey}={OpenNotificationsAction}",
            $"{NotificationIdKey}={notification.Id:D}"
        };

        if (!string.IsNullOrWhiteSpace(notification.ActivationPageKey))
        {
            parts.Add($"{PageKey}={Uri.EscapeDataString(notification.ActivationPageKey.Trim())}");
        }

        return string.Join(';', parts);
    }

    public static AppLaunchActivationRequest? Parse(IEnumerable<string>? args)
    {
        if (args is null)
            return null;

        var request = new AppLaunchActivationRequest(false, null, null);

        foreach (var arg in args.Where(arg => !string.IsNullOrWhiteSpace(arg)))
        {
            if (string.Equals(arg, OpenNotificationsAction, StringComparison.OrdinalIgnoreCase))
            {
                request = request with { OpenNotificationsRequested = true };
                continue;
            }

            foreach (var part in arg.Split([';', '&'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = part.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    if (part.Contains(OpenNotificationsAction, StringComparison.OrdinalIgnoreCase))
                        request = request with { OpenNotificationsRequested = true };
                    continue;
                }

                var key = part[..separatorIndex];
                var value = part[(separatorIndex + 1)..];

                if (key.Equals(ActionKey, StringComparison.OrdinalIgnoreCase) &&
                    value.Equals(OpenNotificationsAction, StringComparison.OrdinalIgnoreCase))
                {
                    request = request with { OpenNotificationsRequested = true };
                    continue;
                }

                if (key.Equals(NotificationIdKey, StringComparison.OrdinalIgnoreCase) &&
                    Guid.TryParse(value, out var notificationId))
                {
                    request = request with { NotificationId = notificationId };
                    continue;
                }

                if (key.Equals(PageKey, StringComparison.OrdinalIgnoreCase))
                {
                    request = request with { PageKey = Uri.UnescapeDataString(value) };
                }
            }
        }

        return request.OpenNotificationsRequested || request.NotificationId.HasValue || !string.IsNullOrWhiteSpace(request.PageKey)
            ? request
            : null;
    }
}
