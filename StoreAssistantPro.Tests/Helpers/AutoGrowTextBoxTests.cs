using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("WpfUi")]
public class AutoGrowTextBoxTests
{
    [Fact]
    public void EnabledMultilineTextBox_GrowsBeyondMinHeight()
    {
        WpfTestApplication.Run(() =>
        {
            var textBox = CreateTextBox(maxHeight: 160);
            var window = CreateHostWindow(textBox);

            try
            {
                textBox.Text = "Line one of a longer note that should wrap onto multiple lines in the form.\nLine two keeps growing the editor height.";
                DrainDispatcher();

                Assert.True(textBox.Height > textBox.MinHeight, $"Expected height greater than min height, but got {textBox.Height}.");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void EnabledMultilineTextBox_RespectsMaxHeight()
    {
        WpfTestApplication.Run(() =>
        {
            var textBox = CreateTextBox(maxHeight: 96);
            var window = CreateHostWindow(textBox);

            try
            {
                textBox.Text = string.Join(Environment.NewLine, Enumerable.Repeat("Wrapped content that keeps extending the editor height.", 12));
                DrainDispatcher();

                Assert.True(textBox.Height <= 96, $"Expected height to stay within the max height, but got {textBox.Height}.");
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static Window CreateHostWindow(TextBox textBox)
    {
        var window = new Window
        {
            Width = 320,
            Height = 240,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            Content = new Grid
            {
                Margin = new Thickness(16),
                Children = { textBox }
            }
        };

        window.Show();
        window.UpdateLayout();
        DrainDispatcher();
        return window;
    }

    private static TextBox CreateTextBox(double maxHeight)
    {
        var textBox = new TextBox
        {
            Width = 240,
            MinHeight = 32,
            MaxHeight = maxHeight,
            Padding = new Thickness(12, 10, 12, 10),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        AutoGrowTextBox.SetIsEnabled(textBox, true);
        return textBox;
    }

    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Render,
            new DispatcherOperationCallback(_ =>
            {
                return null;
            }),
            null);
        Dispatcher.PushFrame(frame);
    }
}
