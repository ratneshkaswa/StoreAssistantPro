using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that shows placeholder/watermark text inside a
/// <see cref="TextBox"/> when empty.  Uses an adorner so no extra
/// elements are needed in the visual tree.
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;TextBox h:Watermark.Text="🔍 Search products..."/&gt;
/// </code>
/// </summary>
public static class Watermark
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(Watermark),
            new PropertyMetadata(null, OnTextChanged));

    public static string? GetText(DependencyObject obj) =>
        (string?)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string? value) =>
        obj.SetValue(TextProperty, value);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb)
            return;

        tb.Loaded -= OnLoaded;
        tb.TextChanged -= OnTextBoxTextChanged;
        tb.GotFocus -= OnGotFocus;
        tb.LostFocus -= OnLostFocus;
        tb.IsVisibleChanged -= OnIsVisibleChanged;

        // Watermark rendering is temporarily disabled app-wide.
        RemoveAdorner(tb);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e) =>
        UpdateAdorner((TextBox)sender);

    private static void OnTextBoxTextChanged(object sender, TextChangedEventArgs e) =>
        UpdateAdorner((TextBox)sender);

    private static void OnGotFocus(object sender, RoutedEventArgs e) =>
        UpdateAdorner((TextBox)sender);

    private static void OnLostFocus(object sender, RoutedEventArgs e) =>
        UpdateAdorner((TextBox)sender);

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        UpdateAdorner((TextBox)sender);

    private static void UpdateAdorner(TextBox tb)
    {
        var layer = AdornerLayer.GetAdornerLayer(tb);
        if (layer is null) return;

        RemoveAdorner(tb, layer);

        if (string.IsNullOrEmpty(tb.Text) && tb.IsVisible && !tb.IsKeyboardFocused)
        {
            var text = GetText(tb);
            if (!string.IsNullOrEmpty(text))
                layer.Add(new WatermarkAdorner(tb, text));
        }
    }

    private static void RemoveAdorner(TextBox tb, AdornerLayer? layer = null)
    {
        layer ??= AdornerLayer.GetAdornerLayer(tb);
        if (layer is null) return;

        var adorners = layer.GetAdorners(tb);
        if (adorners is null) return;

        foreach (var adorner in adorners)
        {
            if (adorner is WatermarkAdorner)
                layer.Remove(adorner);
        }
    }

    private sealed class WatermarkAdorner : Adorner
    {
        private readonly TextBox _adornedTextBox;
        private readonly TextBlock _textBlock;

        public WatermarkAdorner(TextBox adornedElement, string text) : base(adornedElement)
        {
            _adornedTextBox = adornedElement;
            IsHitTestVisible = false;

            var tertiary = adornedElement.TryFindResource("FluentTextTertiary") as Brush
                           ?? new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));

            _textBlock = new TextBlock
            {
                Text = text,
                Foreground = tertiary,
                FontFamily = adornedElement.FontFamily,
                FontSize = adornedElement.FontSize,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(adornedElement.BorderThickness.Left + adornedElement.Padding.Left + 2, 0, 0, 0),
                IsHitTestVisible = false
            };
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _textBlock;

        protected override Size MeasureOverride(Size constraint)
        {
            _textBlock.Measure(constraint);
            return _textBlock.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _textBlock.Measure(finalSize);

            var border = _adornedTextBox.BorderThickness;
            var padding = _adornedTextBox.Padding;

            var x = border.Left + padding.Left;
            var y = (finalSize.Height - _textBlock.DesiredSize.Height) / 2;
            if (y < 0) y = 0;

            var width = finalSize.Width - x - border.Right - 2;
            if (width < 0) width = 0;

            _textBlock.Arrange(new Rect(x, y, width, _textBlock.DesiredSize.Height));
            return finalSize;
        }
    }
}
