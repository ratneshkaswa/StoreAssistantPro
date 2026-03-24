using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Automatically grows multiline text boxes to fit their content with a short height animation.
/// </summary>
public static class AutoGrowTextBox
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(AutoGrowTextBox),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty ObserverProperty =
        DependencyProperty.RegisterAttached(
            "Observer",
            typeof(TextBoxObserver),
            typeof(AutoGrowTextBox),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        if ((bool)e.NewValue)
        {
            if (textBox.GetValue(ObserverProperty) is not TextBoxObserver)
                textBox.SetValue(ObserverProperty, new TextBoxObserver(textBox));

            return;
        }

        if (textBox.GetValue(ObserverProperty) is TextBoxObserver observer)
        {
            observer.Dispose();
            textBox.ClearValue(ObserverProperty);
        }
    }

    private sealed class TextBoxObserver : IDisposable
    {
        private readonly TextBox _textBox;

        public TextBoxObserver(TextBox textBox)
        {
            _textBox = textBox;
            _textBox.Loaded += OnChanged;
            _textBox.Unloaded += OnUnloaded;
            _textBox.TextChanged += OnChanged;
            _textBox.SizeChanged += OnSizeChanged;
            QueueUpdate(false);
        }

        private void OnChanged(object sender, RoutedEventArgs e) => QueueUpdate(true);

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Math.Abs(e.WidthChanged ? e.NewSize.Width - e.PreviousSize.Width : 0) > 0.5)
                QueueUpdate(false);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => _textBox.BeginAnimation(FrameworkElement.HeightProperty, null);

        private void QueueUpdate(bool animate)
        {
            if (!_textBox.Dispatcher.CheckAccess())
            {
                _ = _textBox.Dispatcher.InvokeAsync(() => UpdateHeight(animate));
                return;
            }

            _ = _textBox.Dispatcher.InvokeAsync(() => UpdateHeight(animate), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void UpdateHeight(bool animate)
        {
            if (_textBox.ActualWidth <= 0 || !_textBox.AcceptsReturn || _textBox.TextWrapping == TextWrapping.NoWrap)
                return;

            var targetHeight = MeasureDesiredHeight();
            var minHeight = _textBox.MinHeight > 0 ? _textBox.MinHeight : 32;
            var maxHeight = double.IsInfinity(_textBox.MaxHeight) ? 160d : _textBox.MaxHeight;
            targetHeight = Math.Max(minHeight, Math.Min(maxHeight, targetHeight));

            var currentHeight = double.IsNaN(_textBox.Height)
                ? Math.Max(_textBox.ActualHeight, minHeight)
                : _textBox.Height;

            if (Math.Abs(currentHeight - targetHeight) < 0.5)
            {
                _textBox.BeginAnimation(FrameworkElement.HeightProperty, null);
                _textBox.SetCurrentValue(FrameworkElement.HeightProperty, targetHeight);
                return;
            }

            if (!animate)
            {
                _textBox.BeginAnimation(FrameworkElement.HeightProperty, null);
                _textBox.SetCurrentValue(FrameworkElement.HeightProperty, targetHeight);
                return;
            }

            _textBox.SetCurrentValue(FrameworkElement.HeightProperty, currentHeight);
            _textBox.BeginAnimation(
                FrameworkElement.HeightProperty,
                new DoubleAnimation
                {
                    From = currentHeight,
                    To = targetHeight,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                },
                HandoffBehavior.SnapshotAndReplace);
        }

        private double MeasureDesiredHeight()
        {
            var text = string.IsNullOrEmpty(_textBox.Text) ? " " : _textBox.Text;
            if (text.EndsWith('\n') || text.EndsWith('\r'))
                text += " ";

            var availableWidth = Math.Max(
                0,
                _textBox.ActualWidth -
                _textBox.Padding.Left -
                _textBox.Padding.Right -
                _textBox.BorderThickness.Left -
                _textBox.BorderThickness.Right -
                4);

            var dpi = VisualTreeHelper.GetDpi(_textBox).PixelsPerDip;
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                _textBox.FlowDirection,
                new Typeface(_textBox.FontFamily, _textBox.FontStyle, _textBox.FontWeight, _textBox.FontStretch),
                _textBox.FontSize,
                Brushes.Transparent,
                dpi)
            {
                MaxTextWidth = Math.Max(1, availableWidth),
                Trimming = TextTrimming.None
            };

            var lineHeight = Math.Max(_textBox.FontSize * 1.45, _textBox.FontFamily.LineSpacing * _textBox.FontSize);
            var contentHeight = Math.Max(lineHeight, Math.Ceiling(formattedText.Height));
            return Math.Ceiling(contentHeight + _textBox.Padding.Top + _textBox.Padding.Bottom + _textBox.BorderThickness.Top + _textBox.BorderThickness.Bottom + 2);
        }

        public void Dispose()
        {
            _textBox.Loaded -= OnChanged;
            _textBox.Unloaded -= OnUnloaded;
            _textBox.TextChanged -= OnChanged;
            _textBox.SizeChanged -= OnSizeChanged;
            _textBox.BeginAnimation(FrameworkElement.HeightProperty, null);
        }
    }
}
