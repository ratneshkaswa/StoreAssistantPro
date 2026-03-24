using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Adds a short Win11-style click ripple to button surfaces.
/// </summary>
public static class ClickRipple
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ClickRipple),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
        {
            element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        }
        else
        {
            element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            !element.IsEnabled ||
            element.ActualWidth <= 0 ||
            element.ActualHeight <= 0)
        {
            return;
        }

        var layer = AdornerLayer.GetAdornerLayer(element);
        if (layer is null)
            return;

        var adorner = new RippleAdorner(
            element,
            e.GetPosition(element),
            ResolveRippleBrush(element));

        layer.Add(adorner);
        adorner.Begin(layer);
    }

    private static SolidColorBrush ResolveRippleBrush(FrameworkElement element)
    {
        var accent = TryFindColor(element, "FluentAccentDefault") ?? Color.FromRgb(0x00, 0x5F, 0xB8);
        var surfaceSample = GetBrushSample(element);

        var rippleColor = surfaceSample.HasValue && GetLuminance(surfaceSample.Value) < 0.45
            ? Colors.White
            : accent;

        return new SolidColorBrush(rippleColor) { Opacity = 0.18 };
    }

    private static Color? GetBrushSample(FrameworkElement element)
    {
        return element switch
        {
            Control control => TryGetColor(control.Background),
            Border border => TryGetColor(border.Background),
            _ => null
        };
    }

    private static Color? TryFindColor(FrameworkElement element, string resourceKey) =>
        element.TryFindResource(resourceKey) switch
        {
            SolidColorBrush brush => brush.Color,
            LinearGradientBrush gradient when gradient.GradientStops.Count > 0 => gradient.GradientStops[0].Color,
            _ => null
        };

    private static Color? TryGetColor(Brush? brush) => brush switch
    {
        SolidColorBrush solid when solid.Color.A > 0 => solid.Color,
        LinearGradientBrush gradient when gradient.GradientStops.Count > 0 => gradient.GradientStops[0].Color,
        _ => null
    };

    private static double GetLuminance(Color color) =>
        ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255d;

    private sealed class RippleAdorner : Adorner
    {
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(
                nameof(Radius),
                typeof(double),
                typeof(RippleAdorner),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RippleOpacityProperty =
            DependencyProperty.Register(
                nameof(RippleOpacity),
                typeof(double),
                typeof(RippleAdorner),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        private readonly Point _origin;
        private readonly SolidColorBrush _brush;

        public RippleAdorner(UIElement adornedElement, Point origin, SolidColorBrush brush)
            : base(adornedElement)
        {
            _origin = origin;
            _brush = brush;
            IsHitTestVisible = false;
        }

        public double Radius
        {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public double RippleOpacity
        {
            get => (double)GetValue(RippleOpacityProperty);
            set => SetValue(RippleOpacityProperty, value);
        }

        public void Begin(AdornerLayer layer)
        {
            var size = AdornedElement.RenderSize;
            var maxX = Math.Max(_origin.X, size.Width - _origin.X);
            var maxY = Math.Max(_origin.Y, size.Height - _origin.Y);
            var targetRadius = Math.Sqrt((maxX * maxX) + (maxY * maxY));

            var radiusAnimation = new DoubleAnimation(0, targetRadius, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var opacityAnimation = new DoubleAnimation(0.18, 0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            opacityAnimation.Completed += (_, _) => layer.Remove(this);

            BeginAnimation(RadiusProperty, radiusAnimation);
            BeginAnimation(RippleOpacityProperty, opacityAnimation);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var bounds = new Rect(AdornedElement.RenderSize);
            drawingContext.PushClip(new RectangleGeometry(bounds));

            var brush = _brush.CloneCurrentValue();
            brush.Opacity = RippleOpacity;

            drawingContext.DrawEllipse(brush, null, _origin, Radius, Radius);
            drawingContext.Pop();
        }
    }
}
