using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Enables drag-away dismissal on toast cards. Supports both mouse and touch
/// so transient toasts can be swiped away horizontally without adding
/// explicit close buttons to the compact surface.
/// </summary>
public static class ToastSwipeDismiss
{
    private const double DismissThreshold = 96;
    private const double SwipeOpacityDistance = 180;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ToastSwipeDismiss),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(DragState),
            typeof(ToastSwipeDismiss));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    public static bool ShouldDismiss(double horizontalDelta) =>
        Math.Abs(horizontalDelta) >= DismissThreshold;

    public static double CalculateOpacity(double horizontalDelta)
    {
        var normalized = Math.Min(Math.Abs(horizontalDelta) / SwipeOpacityDistance, 1d);
        return Math.Max(0.35, 1d - (normalized * 0.65));
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
        {
            element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            element.PreviewMouseMove += OnPreviewMouseMove;
            element.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            element.LostMouseCapture += OnLostMouseCapture;
            element.TouchDown += OnTouchDown;
            element.TouchMove += OnTouchMove;
            element.TouchUp += OnTouchUp;
            element.LostTouchCapture += OnLostTouchCapture;
            element.Cursor = Cursors.Hand;
        }
        else
        {
            element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            element.PreviewMouseMove -= OnPreviewMouseMove;
            element.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            element.LostMouseCapture -= OnLostMouseCapture;
            element.TouchDown -= OnTouchDown;
            element.TouchMove -= OnTouchMove;
            element.TouchUp -= OnTouchUp;
            element.LostTouchCapture -= OnLostTouchCapture;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var state = GetOrCreateState(element);
        if (state.IsTouchInteraction)
            return;

        BeginInteraction(element, e.GetPosition(element));
        state.IsTouchInteraction = false;
        element.CaptureMouse();
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var state = GetState(element);
        if (state is null || !state.IsDragging || state.IsTouchInteraction || !element.IsMouseCaptured)
            return;

        UpdateInteraction(element, e.GetPosition(element));
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var state = GetState(element);
        if (state is null || state.IsTouchInteraction || !state.IsDragging)
            return;

        FinishInteraction(element);
        element.ReleaseMouseCapture();
    }

    private static void OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var state = GetState(element);
        if (state is null || state.IsTouchInteraction || !state.IsDragging)
            return;

        FinishInteraction(element);
    }

    private static void OnTouchDown(object? sender, TouchEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var state = GetOrCreateState(element);
        state.IsTouchInteraction = true;
        BeginInteraction(element, e.GetTouchPoint(element).Position);
        element.CaptureTouch(e.TouchDevice);
        e.Handled = true;
    }

    private static void OnTouchMove(object? sender, TouchEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var state = GetState(element);
        if (state is null || !state.IsDragging || !state.IsTouchInteraction)
            return;

        UpdateInteraction(element, e.GetTouchPoint(element).Position);
        e.Handled = true;
    }

    private static void OnTouchUp(object? sender, TouchEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var state = GetState(element);
        if (state is null || !state.IsDragging || !state.IsTouchInteraction)
            return;

        FinishInteraction(element);
        element.ReleaseTouchCapture(e.TouchDevice);
        e.Handled = true;
    }

    private static void OnLostTouchCapture(object? sender, TouchEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        var state = GetState(element);
        if (state is null || !state.IsDragging || !state.IsTouchInteraction)
            return;

        FinishInteraction(element);
    }

    private static void BeginInteraction(FrameworkElement element, Point startPoint)
    {
        var state = GetOrCreateState(element);
        state.StartPoint = startPoint;
        state.CurrentDelta = 0;
        state.IsDragging = true;

        var translate = EnsureTranslateTransform(element);
        translate.X = 0;
        element.Opacity = 1;
    }

    private static void UpdateInteraction(FrameworkElement element, Point currentPoint)
    {
        var state = GetState(element);
        if (state is null)
            return;

        state.CurrentDelta = currentPoint.X - state.StartPoint.X;

        var translate = EnsureTranslateTransform(element);
        translate.X = state.CurrentDelta;
        element.Opacity = CalculateOpacity(state.CurrentDelta);
    }

    private static void FinishInteraction(FrameworkElement element)
    {
        var state = GetState(element);
        if (state is null)
            return;

        state.IsDragging = false;
        state.IsTouchInteraction = false;

        if (ShouldDismiss(state.CurrentDelta))
        {
            DismissWithAnimation(element, state.CurrentDelta);
        }
        else
        {
            ResetInteraction(element);
        }
    }

    private static void ResetInteraction(FrameworkElement element)
    {
        var translate = EnsureTranslateTransform(element);
        translate.X = 0;
        element.Opacity = 1;
    }

    private static void DismissWithAnimation(FrameworkElement element, double currentDelta)
    {
        if (element.DataContext is not ToastItem toast)
        {
            ResetInteraction(element);
            return;
        }

        var service = ResolveToastService();
        if (service is null)
        {
            ResetInteraction(element);
            return;
        }

        var translate = EnsureTranslateTransform(element);
        service.Dismiss(toast.Id);
        element.Opacity = 1;
        translate.X = 0;
    }

    private static IToastService? ResolveToastService()
    {
        if (Application.Current is not App app)
            return null;

        return app.Services?.GetService<IToastService>();
    }

    private static DragState GetOrCreateState(FrameworkElement element)
    {
        var state = GetState(element);
        if (state is not null)
            return state;

        state = new DragState();
        element.SetValue(StateProperty, state);
        return state;
    }

    private static DragState? GetState(FrameworkElement element) =>
        element.GetValue(StateProperty) as DragState;

    private static TranslateTransform EnsureTranslateTransform(FrameworkElement element)
    {
        if (element.RenderTransform is TranslateTransform translate)
            return translate;

        if (element.RenderTransform is TransformGroup group)
        {
            var existing = group.Children.OfType<TranslateTransform>().FirstOrDefault();
            if (existing is not null)
                return existing;

            var appended = new TranslateTransform();
            group.Children.Add(appended);
            return appended;
        }

        if (element.RenderTransform is null || element.RenderTransform.Value.IsIdentity)
        {
            var created = new TranslateTransform();
            element.RenderTransform = created;
            return created;
        }

        var wrappedGroup = new TransformGroup();
        wrappedGroup.Children.Add(element.RenderTransform);
        var wrappedTranslate = new TranslateTransform();
        wrappedGroup.Children.Add(wrappedTranslate);
        element.RenderTransform = wrappedGroup;
        return wrappedTranslate;
    }

    private sealed class DragState
    {
        public Point StartPoint { get; set; }

        public double CurrentDelta { get; set; }

        public bool IsDragging { get; set; }

        public bool IsTouchInteraction { get; set; }
    }
}
