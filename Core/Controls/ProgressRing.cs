using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Windows 11 WinUI-style indeterminate progress ring.
/// Displays a spinning arc that replaces "Loading..." text overlays.
/// <para>
/// Bind <see cref="IsActive"/> to <c>IsLoading</c> on the ViewModel.
/// The ring auto-collapses when inactive.
/// </para>
/// <example>
/// <code>&lt;controls:ProgressRing IsActive="{Binding IsLoading}" Diameter="24"/&gt;</code>
/// </example>
/// </summary>
public class ProgressRing : Control
{
    private Storyboard? _spinStoryboard;
    private Path? _arcPath;

    static ProgressRing()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProgressRing), new FrameworkPropertyMetadata(typeof(ProgressRing)));
    }

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(ProgressRing),
            new PropertyMetadata(false, OnIsActiveChanged));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly DependencyProperty DiameterProperty =
        DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(ProgressRing),
            new PropertyMetadata(32.0, OnDiameterChanged));

    public double Diameter
    {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(ProgressRing),
            new PropertyMetadata(3.0, OnDiameterChanged));

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public static readonly DependencyProperty RingBrushProperty =
        DependencyProperty.Register(nameof(RingBrush), typeof(Brush), typeof(ProgressRing),
            new PropertyMetadata(null));

    public Brush? RingBrush
    {
        get => (Brush?)GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _arcPath = GetTemplateChild("PART_Arc") as Path;
        UpdateArcGeometry();
        UpdateVisualState();
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressRing ring)
            ring.UpdateVisualState();
    }

    private static void OnDiameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressRing ring)
            ring.UpdateArcGeometry();
    }

    private void UpdateArcGeometry()
    {
        if (_arcPath is null) return;

        var radius = (Diameter - StrokeThickness) / 2;
        var center = Diameter / 2;

        // Draw a 270-degree arc
        const double arcAngle = 270.0;
        var startAngle = -90.0; // Start at top
        var endAngle = startAngle + arcAngle;

        var startRad = startAngle * Math.PI / 180;
        var endRad = endAngle * Math.PI / 180;

        var startX = center + radius * Math.Cos(startRad);
        var startY = center + radius * Math.Sin(startRad);
        var endX = center + radius * Math.Cos(endRad);
        var endY = center + radius * Math.Sin(endRad);

        var isLargeArc = arcAngle > 180;

        var figure = new PathFigure
        {
            StartPoint = new Point(startX, startY),
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(radius, radius),
            IsLargeArc = isLargeArc,
            SweepDirection = SweepDirection.Clockwise
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();

        _arcPath.Data = geometry;
        _arcPath.StrokeThickness = StrokeThickness;
        _arcPath.Width = Diameter;
        _arcPath.Height = Diameter;
    }

    private void UpdateVisualState()
    {
        if (_arcPath is null) return;

        if (IsActive)
        {
            Visibility = Visibility.Visible;
            StartSpinAnimation();
        }
        else
        {
            StopSpinAnimation();
            Visibility = Visibility.Collapsed;
        }
    }

    private void StartSpinAnimation()
    {
        if (_arcPath is null) return;

        StopSpinAnimation();

        var center = Diameter / 2;
        _arcPath.RenderTransformOrigin = new Point(0.5, 0.5);
        _arcPath.RenderTransform = new RotateTransform(0);

        var animation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = new Duration(TimeSpan.FromMilliseconds(1100)),
            RepeatBehavior = RepeatBehavior.Forever
        };

        _spinStoryboard = new Storyboard();
        Storyboard.SetTarget(animation, _arcPath);
        Storyboard.SetTargetProperty(animation,
            new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
        _spinStoryboard.Children.Add(animation);
        _spinStoryboard.Begin();
    }

    private void StopSpinAnimation()
    {
        _spinStoryboard?.Stop();
        _spinStoryboard = null;
    }
}
