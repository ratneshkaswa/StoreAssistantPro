using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class WindowsNotificationLaunchArgumentsTests
{
    [Fact]
    public void Build_And_Parse_Should_RoundTrip_Notification_Metadata()
    {
        var notification = new AppNotification
        {
            Id = Guid.Parse("B94A3E6B-3182-4F2B-9A9F-1C3FA5B5D3C1"),
            Title = "Payment received",
            Message = "Invoice INV-1042 was paid.",
            Timestamp = new DateTime(2026, 3, 25, 10, 15, 0),
            Level = AppNotificationLevel.Success,
            ActivationPageKey = "Reports"
        };

        var args = WindowsNotificationLaunchArguments.Build(notification);
        var parsed = WindowsNotificationLaunchArguments.Parse([args]);

        Assert.NotNull(parsed);
        Assert.True(parsed!.OpenNotificationsRequested);
        Assert.Equal(notification.Id, parsed.NotificationId);
        Assert.Equal("Reports", parsed.PageKey);
    }
}
