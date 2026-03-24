using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// A <see cref="ContentControl"/> designed for the MainWindow content region
/// that stretches its content to fill available space while enabling vertical
/// scrolling when the content's desired height exceeds the viewport.
/// </summary>
public class ResponsiveContentControl : ContentControl
{
    private const string PartScrollViewer = "PART_ScrollViewer";
    private const string PartTransitionHost = "PART_TransitionHost";
    private const string PartCurrentPresenter = "PART_CurrentPresenter";
    private const string PartPreviousSnapshot = "PART_PreviousSnapshot";

    private const string PageExitDurationKey = "FluentDurationPageExit";
    private const string PageExitOffsetKey = "MotionSlideOffsetExitSmall";
    private const string PageExitEaseKey = "FluentEaseAccelerate";
    private const string ViewSwitchEnterDurationKey = "FluentDurationViewSwitchEnter";
    private const string ViewSwitchEnterDelayKey = "FluentDurationViewSwitchOverlap";
    private const string ViewSwitchEnterEaseKey = "FluentEaseDecelerate";

    public static readonly DependencyProperty PreviousSnapshotProperty =
        DependencyProperty.Register(
            nameof(PreviousSnapshot),
            typeof(ImageSource),
            typeof(ResponsiveContentControl),
            new PropertyMetadata(null));

    private ScrollViewer? _scrollViewer;
    private FrameworkElement? _transitionHost;
    private ContentPresenter? _currentPresenter;
    private Image? _previousSnapshotImage;
    private int _transitionVersion;

    static ResponsiveContentControl()
    {
        FocusableProperty.OverrideMetadata(
            typeof(ResponsiveContentControl),
            new FrameworkPropertyMetadata(false));
    }

    public ImageSource? PreviousSnapshot
    {
        get => (ImageSource?)GetValue(PreviousSnapshotProperty);
        private set => SetValue(PreviousSnapshotProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
        _transitionHost = GetTemplateChild(PartTransitionHost) as FrameworkElement;
        _currentPresenter = GetTemplateChild(PartCurrentPresenter) as ContentPresenter;
        _previousSnapshotImage = GetTemplateChild(PartPreviousSnapshot) as Image;

        ResetCurrentPresenter();
        ClearTransitionState();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        var previousSnapshot = CaptureCurrentSnapshot(oldContent, newContent);

        base.OnContentChanged(oldContent, newContent);
        _scrollViewer?.ScrollToTop();

        if (previousSnapshot is null ||
            _previousSnapshotImage is null ||
            _currentPresenter is null ||
            !IsLoaded ||
            oldContent is null ||
            newContent is null ||
            ReferenceEquals(oldContent, newContent))
        {
            ResetCurrentPresenter();
            ClearTransitionState();
            return;
        }

        var version = ++_transitionVersion;
        StartCurrentEnterTransition(version);
        StartSnapshotExitTransition(previousSnapshot, version);
    }

    private RenderTargetBitmap? CaptureCurrentSnapshot(object oldContent, object newContent)
    {
        if (_transitionHost is null ||
            oldContent is null ||
            newContent is null ||
            ReferenceEquals(oldContent, newContent) ||
            !IsLoaded)
        {
            return null;
        }

        CancelActiveTransition();

        if (_transitionHost.ActualWidth < 1 || _transitionHost.ActualHeight < 1)
        {
            return null;
        }

        var dpi = VisualTreeHelper.GetDpi(_transitionHost);
        var width = Math.Max(1, (int)Math.Ceiling(_transitionHost.ActualWidth * dpi.DpiScaleX));
        var height = Math.Max(1, (int)Math.Ceiling(_transitionHost.ActualHeight * dpi.DpiScaleY));

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(
                new VisualBrush(_transitionHost),
                null,
                new Rect(0, 0, _transitionHost.ActualWidth, _transitionHost.ActualHeight));
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

    private void StartSnapshotExitTransition(ImageSource snapshot, int version)
    {
        if (_previousSnapshotImage is null)
        {
            return;
        }

        PreviousSnapshot = snapshot;
        _previousSnapshotImage.Visibility = Visibility.Visible;
        _previousSnapshotImage.Opacity = 1;

        var translate = EnsureTranslateTransform(_previousSnapshotImage);
        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.Y = 0;

        var duration = GetDuration(PageExitDurationKey, TimeSpan.FromMilliseconds(100));
        var easing = TryFindEase(PageExitEaseKey);
        var exitOffset = GetDouble(PageExitOffsetKey, -8);

        if (duration == TimeSpan.Zero)
        {
            CompleteSnapshotExit(version);
            return;
        }

        var fadeAnimation = new DoubleAnimation(1, 0, new Duration(duration))
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.Stop
        };
        fadeAnimation.Completed += (_, _) => CompleteSnapshotExit(version);
        _previousSnapshotImage.BeginAnimation(OpacityProperty, fadeAnimation);

        var slideAnimation = new DoubleAnimation(0, exitOffset, new Duration(duration))
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.Stop
        };
        translate.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
    }

    private void StartCurrentEnterTransition(int version)
    {
        if (_currentPresenter is null)
        {
            return;
        }

        _currentPresenter.BeginAnimation(OpacityProperty, null);
        _currentPresenter.Opacity = 0;

        var translate = EnsureTranslateTransform(_currentPresenter);
        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.Y = 0;

        var duration = GetDuration(ViewSwitchEnterDurationKey, TimeSpan.FromMilliseconds(150));
        var overlap = GetDuration(ViewSwitchEnterDelayKey, TimeSpan.FromMilliseconds(50));
        var easing = TryFindEase(ViewSwitchEnterEaseKey);

        if (duration == TimeSpan.Zero)
        {
            ResetCurrentPresenter();
            return;
        }

        var fadeAnimation = new DoubleAnimation(0, 1, new Duration(duration))
        {
            BeginTime = overlap,
            EasingFunction = easing,
            FillBehavior = FillBehavior.Stop
        };
        fadeAnimation.Completed += (_, _) => CompleteCurrentEnter(version);
        _currentPresenter.BeginAnimation(OpacityProperty, fadeAnimation);
    }

    private void CompleteSnapshotExit(int version)
    {
        if (version != _transitionVersion)
        {
            return;
        }

        ClearTransitionState();
    }

    private void CompleteCurrentEnter(int version)
    {
        if (version != _transitionVersion)
        {
            return;
        }

        ResetCurrentPresenter();
    }

    private void ResetCurrentPresenter()
    {
        if (_currentPresenter is null)
        {
            return;
        }

        _currentPresenter.BeginAnimation(OpacityProperty, null);
        _currentPresenter.Opacity = 1;

        var translate = EnsureTranslateTransform(_currentPresenter);
        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.Y = 0;
    }

    private void CancelActiveTransition()
    {
        _transitionVersion++;
        ClearTransitionState();
    }

    private void ClearTransitionState()
    {
        PreviousSnapshot = null;

        if (_previousSnapshotImage is null)
        {
            return;
        }

        _previousSnapshotImage.BeginAnimation(OpacityProperty, null);
        _previousSnapshotImage.Opacity = 1;
        _previousSnapshotImage.Visibility = Visibility.Collapsed;

        var translate = EnsureTranslateTransform(_previousSnapshotImage);
        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.Y = 0;
    }

    private TimeSpan GetDuration(string key, TimeSpan fallback)
    {
        if (TryFindResource(key) is Duration duration && duration.HasTimeSpan)
        {
            return duration.TimeSpan;
        }

        return fallback;
    }

    private double GetDouble(string key, double fallback)
    {
        if (TryFindResource(key) is double value)
        {
            return value;
        }

        return fallback;
    }

    private IEasingFunction? TryFindEase(string key) => TryFindResource(key) as IEasingFunction;

    private static TranslateTransform EnsureTranslateTransform(FrameworkElement element)
    {
        if (element.RenderTransform is TranslateTransform translate)
        {
            return translate;
        }

        if (element.RenderTransform is TransformGroup group)
        {
            var existingTranslate = group.Children.OfType<TranslateTransform>().FirstOrDefault();
            if (existingTranslate is not null)
            {
                return existingTranslate;
            }

            var appendedTranslate = new TranslateTransform();
            group.Children.Add(appendedTranslate);
            element.RenderTransform = group;
            return appendedTranslate;
        }

        if (element.RenderTransform is null || element.RenderTransform.Value.IsIdentity)
        {
            var newTranslate = new TranslateTransform();
            element.RenderTransform = newTranslate;
            return newTranslate;
        }

        var wrappedGroup = new TransformGroup();
        wrappedGroup.Children.Add(element.RenderTransform);
        var wrappedTranslate = new TranslateTransform();
        wrappedGroup.Children.Add(wrappedTranslate);
        element.RenderTransform = wrappedGroup;
        return wrappedTranslate;
    }
}
