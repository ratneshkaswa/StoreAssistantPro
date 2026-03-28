using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
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

    protected override AutomationPeer OnCreateAutomationPeer() => new ProgressRingAutomationPeer(this);

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
        if (_arcPath is null)
            return;

        var radius = (Diameter - StrokeThickness) / 2;
        var center = Diameter / 2;

        const double arcAngle = 270.0;
        const double startAngle = -90.0;
        var endAngle = startAngle + arcAngle;

        var startRad = startAngle * Math.PI / 180;
        var endRad = endAngle * Math.PI / 180;

        var startX = center + radius * Math.Cos(startRad);
        var startY = center + radius * Math.Sin(startRad);
        var endX = center + radius * Math.Cos(endRad);
        var endY = center + radius * Math.Sin(endRad);

        var figure = new PathFigure
        {
            StartPoint = new Point(startX, startY),
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(radius, radius),
            IsLargeArc = arcAngle > 180,
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
        if (_arcPath is null)
            return;

        if (IsActive)
        {
            Visibility = Visibility.Visible;
            ApplyActiveState();
        }
        else
        {
            ApplyInactiveState();
            Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyActiveState()
    {
        if (_arcPath is null)
            return;

        _arcPath.ClearValue(UIElement.RenderTransformProperty);
        _arcPath.ClearValue(UIElement.RenderTransformOriginProperty);
        _arcPath.Opacity = 1;
    }

    private void ApplyInactiveState()
    {
        if (_arcPath is null)
            return;

        _arcPath.ClearValue(UIElement.RenderTransformProperty);
        _arcPath.ClearValue(UIElement.RenderTransformOriginProperty);
        _arcPath.Opacity = 1;
    }

    private sealed class ProgressRingAutomationPeer(ProgressRing owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(ProgressRing);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            return string.IsNullOrWhiteSpace(explicitName) ? "Loading progress" : explicitName;
        }
    }
}
