using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Captures the last clicked navigation surface so the shell host can run a connected transition.
/// </summary>
public static class ConnectedNavigationSource
{
    private static readonly Dictionary<Window, ConnectedNavigationSnapshot> Snapshots = [];

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ConnectedNavigationSource),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    internal static bool TryConsume(Window window, out ConnectedNavigationSnapshot snapshot)
    {
        if (Snapshots.Remove(window, out snapshot!) &&
            DateTimeOffset.UtcNow - snapshot.CapturedAt <= TimeSpan.FromSeconds(1))
        {
            return true;
        }

        snapshot = null!;
        return false;
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
        {
            element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            element.PreviewKeyDown += OnPreviewKeyDown;
        }
        else
        {
            element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            element.PreviewKeyDown -= OnPreviewKeyDown;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element)
            Capture(element);
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        if (sender is FrameworkElement element)
            Capture(element);
    }

    private static void Capture(FrameworkElement element)
    {
        if (!element.IsLoaded || element.ActualWidth < 1 || element.ActualHeight < 1)
            return;

        var window = Window.GetWindow(element);
        if (window is null || !window.IsLoaded)
            return;

        var bitmap = CaptureSnapshot(element);
        if (bitmap is null)
            return;

        var bounds = element.TransformToAncestor(window)
            .TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

        Snapshots[window] = new ConnectedNavigationSnapshot(bitmap, bounds, DateTimeOffset.UtcNow);
    }

    private static RenderTargetBitmap? CaptureSnapshot(FrameworkElement element)
    {
        var dpi = VisualTreeHelper.GetDpi(element);
        var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX));
        var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY));

        if (width < 1 || height < 1)
            return null;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(
                new VisualBrush(element),
                null,
                new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        }

        var bitmap = new RenderTargetBitmap(
            width,
            height,
            96d * dpi.DpiScaleX,
            96d * dpi.DpiScaleY,
            PixelFormats.Pbgra32);

        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}

internal sealed record ConnectedNavigationSnapshot(
    ImageSource Snapshot,
    Rect SourceBounds,
    DateTimeOffset CapturedAt);
