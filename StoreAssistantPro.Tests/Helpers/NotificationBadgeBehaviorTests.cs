using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class NotificationBadgeBehaviorTests
{
    /// <summary>
    /// All tests run on the STA thread required by WPF.
    /// </summary>
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (caught is not null)
            throw new AggregateException(caught);
    }

    [Fact]
    public void Count_Zero_BadgeIsCollapsed()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 0);

            // Badge should not be created for count 0, or should be Collapsed
            var badge = FindBadge(panel);
            if (badge is not null)
                Assert.Equal(Visibility.Collapsed, badge.Visibility);
        });
    }

    [Fact]
    public void Count_PositiveValue_BadgeIsVisible()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 5);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            Assert.Equal(Visibility.Visible, badge.Visibility);
        });
    }

    [Fact]
    public void Count_ShowsCorrectText()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 7);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            var tb = badge.Child as TextBlock;
            Assert.NotNull(tb);
            Assert.Equal("7", tb.Text);
        });
    }

    [Fact]
    public void Count_Over99_Shows99Plus()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 150);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            var tb = badge.Child as TextBlock;
            Assert.NotNull(tb);
            Assert.Equal("99+", tb.Text);
        });
    }

    [Fact]
    public void Count_TransitionToZero_HidesBadge()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 3);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            Assert.Equal(Visibility.Visible, badge.Visibility);

            NotificationBadgeBehavior.SetCount(panel, 0);
            Assert.Equal(Visibility.Collapsed, badge.Visibility);
        });
    }

    [Fact]
    public void Count_TransitionFromZero_ShowsBadge()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 0);
            NotificationBadgeBehavior.SetCount(panel, 1);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            Assert.Equal(Visibility.Visible, badge.Visibility);
        });
    }

    [Fact]
    public void BadgeBackground_CustomColor_Applied()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            var brush = new SolidColorBrush(Colors.Blue);
            NotificationBadgeBehavior.SetBadgeBackground(panel, brush);
            NotificationBadgeBehavior.SetCount(panel, 1);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            Assert.Same(brush, badge.Background);
        });
    }

    [Fact]
    public void BadgeForeground_CustomColor_Applied()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            var brush = new SolidColorBrush(Colors.Yellow);
            NotificationBadgeBehavior.SetBadgeForeground(panel, brush);
            NotificationBadgeBehavior.SetCount(panel, 1);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            var tb = badge.Child as TextBlock;
            Assert.NotNull(tb);
            Assert.Same(brush, tb.Foreground);
        });
    }

    [Fact]
    public void Badge_RuntimeVisuals_Should_Enable_CrispLayout_And_TextRendering()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 1);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            var tb = badge.Child as TextBlock;
            Assert.NotNull(tb);

            Assert.True(badge.UseLayoutRounding);
            Assert.True(badge.SnapsToDevicePixels);
            Assert.Equal(TextFormattingMode.Display, TextOptions.GetTextFormattingMode(badge));
            Assert.Equal(TextRenderingMode.ClearType, TextOptions.GetTextRenderingMode(badge));

            Assert.True(tb.UseLayoutRounding);
            Assert.True(tb.SnapsToDevicePixels);
            Assert.Equal(TextFormattingMode.Display, TextOptions.GetTextFormattingMode(tb));
            Assert.Equal(TextRenderingMode.ClearType, TextOptions.GetTextRenderingMode(tb));
        });
    }

    [Fact]
    public void Badge_IsNotHitTestVisible()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 1);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            Assert.False(badge.IsHitTestVisible);
        });
    }

    [Fact]
    public void Badge_ReusesSameElement_OnMultipleUpdates()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetCount(panel, 1);
            var badge1 = FindBadge(panel);

            NotificationBadgeBehavior.SetCount(panel, 5);
            var badge2 = FindBadge(panel);

            Assert.NotNull(badge1);
            Assert.Same(badge1, badge2);
        });
    }

    [Fact]
    public void DotOnlyMode_ShowsCompactDotWithoutCountText()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetDotOnly(panel, true);
            NotificationBadgeBehavior.SetCount(panel, 5);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            Assert.Equal(new CornerRadius(4), badge.CornerRadius);
            Assert.Equal(8, badge.Width);
            Assert.Equal(8, badge.Height);

            var tb = badge.Child as TextBlock;
            Assert.NotNull(tb);
            Assert.Equal(Visibility.Collapsed, tb.Visibility);
            Assert.Equal(string.Empty, tb.Text);
        });
    }

    [Fact]
    public void DotOnlyMode_CanSwitchBackToNumericBadge()
    {
        RunOnSta(() =>
        {
            var panel = new Grid();
            NotificationBadgeBehavior.SetDotOnly(panel, true);
            NotificationBadgeBehavior.SetCount(panel, 3);

            NotificationBadgeBehavior.SetDotOnly(panel, false);

            var badge = FindBadge(panel);
            Assert.NotNull(badge);
            Assert.Equal(new CornerRadius(8), badge.CornerRadius);
            Assert.Equal(16, badge.Height);

            var tb = badge.Child as TextBlock;
            Assert.NotNull(tb);
            Assert.Equal(Visibility.Visible, tb.Visibility);
            Assert.Equal("3", tb.Text);
        });
    }

    private static Border? FindBadge(Grid panel)
    {
        foreach (UIElement child in panel.Children)
        {
            if (child is Border border && border.Child is TextBlock)
                return border;
        }
        return null;
    }
}
