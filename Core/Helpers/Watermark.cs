using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that renders watermark text for shared input controls.
/// </summary>
public static class Watermark
{
    private static readonly Regex TrailingDecorationPattern =
        new(@"\s*\([^)]*\)\s*$|\s*[:*]+\s*$", RegexOptions.Compiled);

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(Watermark),
            new PropertyMetadata(null, OnWatermarkOptionsChanged));

    public static readonly DependencyProperty UseFallbackTextProperty =
        DependencyProperty.RegisterAttached(
            "UseFallbackText",
            typeof(bool),
            typeof(Watermark),
            new PropertyMetadata(false, OnWatermarkOptionsChanged));

    private static readonly DependencyProperty HostedOwnerProperty =
        DependencyProperty.RegisterAttached(
            "HostedOwner",
            typeof(Control),
            typeof(Watermark),
            new PropertyMetadata(null));

    public static string? GetText(DependencyObject obj) =>
        (string?)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string? value) =>
        obj.SetValue(TextProperty, value);

    public static bool GetUseFallbackText(DependencyObject obj) =>
        (bool)obj.GetValue(UseFallbackTextProperty);

    public static void SetUseFallbackText(DependencyObject obj, bool value) =>
        obj.SetValue(UseFallbackTextProperty, value);

    private static Control? GetHostedOwner(DependencyObject obj) =>
        obj.GetValue(HostedOwnerProperty) as Control;

    private static void SetHostedOwner(DependencyObject obj, Control? value) =>
        obj.SetValue(HostedOwnerProperty, value);

    private static void OnWatermarkOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        switch (d)
        {
            case TextBox textBox:
                ConfigureTextBox(textBox);
                break;
            case PasswordBox passwordBox:
                ConfigurePasswordBox(passwordBox);
                break;
            case ComboBox comboBox:
                ConfigureComboBox(comboBox);
                break;
            case DatePicker datePicker:
                ConfigureDatePicker(datePicker);
                break;
        }
    }

    private static void ConfigureTextBox(TextBox textBox)
    {
        textBox.Loaded -= OnControlLoaded;
        textBox.Unloaded -= OnControlUnloaded;
        textBox.TextChanged -= OnTextBoxTextChanged;
        textBox.GotKeyboardFocus -= OnControlKeyboardFocusChanged;
        textBox.LostKeyboardFocus -= OnControlKeyboardFocusChanged;
        textBox.IsVisibleChanged -= OnControlIsVisibleChanged;

        if (!HasWatermarkText(textBox))
        {
            RemoveAdorner(textBox);
            return;
        }

        textBox.Loaded += OnControlLoaded;
        textBox.Unloaded += OnControlUnloaded;
        textBox.TextChanged += OnTextBoxTextChanged;
        textBox.GotKeyboardFocus += OnControlKeyboardFocusChanged;
        textBox.LostKeyboardFocus += OnControlKeyboardFocusChanged;
        textBox.IsVisibleChanged += OnControlIsVisibleChanged;
        UpdateAdorner(textBox);
    }

    private static void ConfigurePasswordBox(PasswordBox passwordBox)
    {
        passwordBox.Loaded -= OnControlLoaded;
        passwordBox.Unloaded -= OnControlUnloaded;
        passwordBox.PasswordChanged -= OnPasswordChanged;
        passwordBox.GotKeyboardFocus -= OnControlKeyboardFocusChanged;
        passwordBox.LostKeyboardFocus -= OnControlKeyboardFocusChanged;
        passwordBox.IsVisibleChanged -= OnControlIsVisibleChanged;

        if (!HasWatermarkText(passwordBox))
        {
            RemoveAdorner(passwordBox);
            return;
        }

        passwordBox.Loaded += OnControlLoaded;
        passwordBox.Unloaded += OnControlUnloaded;
        passwordBox.PasswordChanged += OnPasswordChanged;
        passwordBox.GotKeyboardFocus += OnControlKeyboardFocusChanged;
        passwordBox.LostKeyboardFocus += OnControlKeyboardFocusChanged;
        passwordBox.IsVisibleChanged += OnControlIsVisibleChanged;
        UpdateAdorner(passwordBox);
    }

    private static void ConfigureComboBox(ComboBox comboBox)
    {
        comboBox.Loaded -= OnControlLoaded;
        comboBox.Unloaded -= OnControlUnloaded;
        comboBox.SelectionChanged -= OnSelectionChanged;
        comboBox.GotKeyboardFocus -= OnControlKeyboardFocusChanged;
        comboBox.LostKeyboardFocus -= OnControlKeyboardFocusChanged;
        comboBox.IsVisibleChanged -= OnControlIsVisibleChanged;
        UnhookHostedTextBox(comboBox, "PART_EditableTextBox");

        if (!HasWatermarkText(comboBox))
        {
            RemoveAdorner(comboBox);
            return;
        }

        comboBox.Loaded += OnControlLoaded;
        comboBox.Unloaded += OnControlUnloaded;
        comboBox.SelectionChanged += OnSelectionChanged;
        comboBox.GotKeyboardFocus += OnControlKeyboardFocusChanged;
        comboBox.LostKeyboardFocus += OnControlKeyboardFocusChanged;
        comboBox.IsVisibleChanged += OnControlIsVisibleChanged;
        HookHostedTextBox(comboBox, "PART_EditableTextBox");
        UpdateAdorner(comboBox);
    }

    private static void ConfigureDatePicker(DatePicker datePicker)
    {
        datePicker.Loaded -= OnControlLoaded;
        datePicker.Unloaded -= OnControlUnloaded;
        datePicker.SelectedDateChanged -= OnSelectionChanged;
        datePicker.GotKeyboardFocus -= OnControlKeyboardFocusChanged;
        datePicker.LostKeyboardFocus -= OnControlKeyboardFocusChanged;
        datePicker.IsVisibleChanged -= OnControlIsVisibleChanged;
        UnhookHostedTextBox(datePicker, "PART_TextBox");

        if (!HasWatermarkText(datePicker))
        {
            RemoveAdorner(datePicker);
            return;
        }

        datePicker.Loaded += OnControlLoaded;
        datePicker.Unloaded += OnControlUnloaded;
        datePicker.SelectedDateChanged += OnSelectionChanged;
        datePicker.GotKeyboardFocus += OnControlKeyboardFocusChanged;
        datePicker.LostKeyboardFocus += OnControlKeyboardFocusChanged;
        datePicker.IsVisibleChanged += OnControlIsVisibleChanged;
        HookHostedTextBox(datePicker, "PART_TextBox");
        UpdateAdorner(datePicker);
    }

    private static void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        switch (sender)
        {
            case ComboBox comboBox:
                HookHostedTextBox(comboBox, "PART_EditableTextBox");
                UpdateAdorner(comboBox);
                break;
            case DatePicker datePicker:
                HookHostedTextBox(datePicker, "PART_TextBox");
                UpdateAdorner(datePicker);
                break;
            case Control control:
                UpdateAdorner(control);
                break;
        }
    }

    private static void OnControlUnloaded(object? sender, RoutedEventArgs e)
    {
        switch (sender)
        {
            case ComboBox comboBox:
                UnhookHostedTextBox(comboBox, "PART_EditableTextBox");
                RemoveAdorner(comboBox);
                break;
            case DatePicker datePicker:
                UnhookHostedTextBox(datePicker, "PART_TextBox");
                RemoveAdorner(datePicker);
                break;
            case Control control:
                RemoveAdorner(control);
                break;
        }
    }

    private static void OnTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
            UpdateAdorner(textBox);
    }

    private static void OnPasswordChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
            UpdateAdorner(passwordBox);
    }

    private static void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is Control control)
            UpdateAdorner(control);
    }

    private static void OnControlKeyboardFocusChanged(object? sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Control control)
            UpdateAdorner(control);
    }

    private static void OnControlIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is Control control)
            UpdateAdorner(control);
    }

    private static void OnHostedEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is DependencyObject dependencyObject &&
            GetHostedOwner(dependencyObject) is Control control)
        {
            UpdateAdorner(control);
        }
    }

    private static void OnHostedEditorFocusChanged(object? sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is DependencyObject dependencyObject &&
            GetHostedOwner(dependencyObject) is Control control)
        {
            UpdateAdorner(control);
        }
    }

    private static void HookHostedTextBox(Control owner, string partName)
    {
        if (owner.Template?.FindName(partName, owner) is not TextBox textBox)
            return;

        textBox.TextChanged -= OnHostedEditorTextChanged;
        textBox.GotKeyboardFocus -= OnHostedEditorFocusChanged;
        textBox.LostKeyboardFocus -= OnHostedEditorFocusChanged;
        SetHostedOwner(textBox, owner);
        textBox.TextChanged += OnHostedEditorTextChanged;
        textBox.GotKeyboardFocus += OnHostedEditorFocusChanged;
        textBox.LostKeyboardFocus += OnHostedEditorFocusChanged;
    }

    private static void UnhookHostedTextBox(Control owner, string partName)
    {
        if (owner.Template?.FindName(partName, owner) is not TextBox textBox)
            return;

        textBox.TextChanged -= OnHostedEditorTextChanged;
        textBox.GotKeyboardFocus -= OnHostedEditorFocusChanged;
        textBox.LostKeyboardFocus -= OnHostedEditorFocusChanged;
        SetHostedOwner(textBox, null);
    }

    private static bool HasWatermarkText(Control control) =>
        !string.IsNullOrWhiteSpace(ResolveWatermarkText(control));

    private static string? ResolveWatermarkText(Control control)
    {
        var explicitText = GetText(control);
        if (!string.IsNullOrWhiteSpace(explicitText))
            return explicitText;

        if (!GetUseFallbackText(control))
            return null;

        return GetAccessibilityText(control)
               ?? GetGridSiblingLabelText(control)
               ?? Sanitize(control.Name);
    }

    private static string? GetAccessibilityText(FrameworkElement element)
    {
        var automationName = Sanitize(AutomationProperties.GetName(element));
        if (!string.IsNullOrWhiteSpace(automationName))
            return automationName;

        return Sanitize(ExtractText(AutomationProperties.GetLabeledBy(element)));
    }

    private static string? GetGridSiblingLabelText(FrameworkElement element)
    {
        if (VisualTreeHelper.GetParent(element) is not Grid grid)
            return null;

        var row = Grid.GetRow(element);
        var column = Grid.GetColumn(element);

        foreach (var sibling in grid.Children
                     .OfType<FrameworkElement>()
                     .Where(child => child != element &&
                                     Grid.GetRow(child) == row &&
                                     Grid.GetColumn(child) <= column)
                     .OrderByDescending(Grid.GetColumn))
        {
            var text = Sanitize(ExtractText(sibling));
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }

    private static string? ExtractText(object? source) =>
        source switch
        {
            TextBlock textBlock when !string.IsNullOrWhiteSpace(textBlock.Text) => textBlock.Text,
            TextBlock textBlock => string.Concat(textBlock.Inlines.Select(inline => inline switch
            {
                Run run => run.Text,
                _ => string.Empty
            })),
            HeaderedContentControl headeredContentControl => headeredContentControl.Header?.ToString(),
            ContentControl contentControl => contentControl.Content?.ToString(),
            _ => null
        };

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = value.Trim();
        while (true)
        {
            var updated = TrailingDecorationPattern.Replace(cleaned, string.Empty).Trim();
            if (updated == cleaned)
                break;

            cleaned = updated;
        }

        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static void UpdateAdorner(Control control)
    {
        var layer = AdornerLayer.GetAdornerLayer(control);
        if (layer is null)
            return;

        RemoveAdorner(control, layer);

        var text = ResolveWatermarkText(control);
        if (string.IsNullOrWhiteSpace(text) || !ShouldShowWatermark(control))
            return;

        layer.Add(new WatermarkAdorner(
            control,
            text,
            isTopAligned: control is TextBox textBox && textBox.AcceptsReturn,
            trailingReservedSpace: GetTrailingReservedSpace(control)));
    }

    private static bool ShouldShowWatermark(Control control) =>
        control.IsEnabled &&
        control.IsVisible &&
        !control.IsKeyboardFocusWithin &&
        control switch
        {
            TextBox textBox => string.IsNullOrWhiteSpace(textBox.Text),
            PasswordBox passwordBox => string.IsNullOrWhiteSpace(passwordBox.Password),
            ComboBox comboBox => string.IsNullOrWhiteSpace(GetComboBoxText(comboBox)),
            DatePicker datePicker => string.IsNullOrWhiteSpace(GetDatePickerText(datePicker)),
            _ => false
        };

    private static string GetComboBoxText(ComboBox comboBox)
    {
        if (comboBox.IsEditable &&
            comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox editableTextBox)
        {
            return editableTextBox.Text ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(comboBox.Text))
            return comboBox.Text;

        return comboBox.SelectionBoxItem?.ToString() ?? string.Empty;
    }

    private static string GetDatePickerText(DatePicker datePicker)
    {
        if (datePicker.Template?.FindName("PART_TextBox", datePicker) is DatePickerTextBox textBox)
            return textBox.Text ?? string.Empty;

        return datePicker.Text ?? string.Empty;
    }

    private static double GetTrailingReservedSpace(Control control) =>
        control switch
        {
            ComboBox => 36,
            DatePicker => 40,
            _ => 0
        };

    private static Rect GetContentBounds(Control control, Size finalSize, double trailingReservedSpace)
    {
        if (TryGetHostedContentBounds(control, out var hostedBounds))
            return ClampBounds(hostedBounds, finalSize);

        return ClampBounds(
            InsetBounds(new Rect(new Point(0, 0), finalSize), control.BorderThickness, control.Padding, trailingReservedSpace),
            finalSize);
    }

    private static bool TryGetHostedContentBounds(Control control, out Rect bounds)
    {
        switch (control)
        {
            case TextBox textBox:
                return TryGetScrollViewerContentBounds(textBox, "PART_ContentHost", out bounds);
            case PasswordBox passwordBox:
                return TryGetScrollViewerContentBounds(passwordBox, "PART_ContentHost", out bounds);
            case ComboBox comboBox:
                return TryGetComboBoxContentBounds(comboBox, out bounds);
            case DatePicker datePicker:
                return TryGetDatePickerContentBounds(datePicker, out bounds);
            default:
                bounds = Rect.Empty;
                return false;
        }
    }

    private static bool TryGetScrollViewerContentBounds(Control owner, string partName, out Rect bounds)
    {
        if (owner.Template?.FindName(partName, owner) is ScrollViewer contentHost &&
            TryGetBoundsRelativeToControl(contentHost, owner, out var contentHostBounds))
        {
            bounds = InsetBounds(contentHostBounds, new Thickness(0), contentHost.Padding, 0);
            return true;
        }

        bounds = Rect.Empty;
        return false;
    }

    private static bool TryGetComboBoxContentBounds(ComboBox comboBox, out Rect bounds)
    {
        if (comboBox.IsEditable &&
            comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox editableTextBox &&
            TryGetBoundsRelativeToControl(editableTextBox, comboBox, out var editableBounds))
        {
            bounds = InsetBounds(editableBounds, editableTextBox.BorderThickness, editableTextBox.Padding, 0);
            return true;
        }

        if (comboBox.Template?.FindName("ContentSite", comboBox) is FrameworkElement contentSite &&
            TryGetBoundsRelativeToControl(contentSite, comboBox, out bounds))
        {
            return true;
        }

        bounds = Rect.Empty;
        return false;
    }

    private static bool TryGetDatePickerContentBounds(DatePicker datePicker, out Rect bounds)
    {
        if (datePicker.Template?.FindName("PART_TextBox", datePicker) is DatePickerTextBox textBox &&
            TryGetBoundsRelativeToControl(textBox, datePicker, out var textBoxBounds))
        {
            bounds = InsetBounds(textBoxBounds, textBox.BorderThickness, textBox.Padding, 0);
            return true;
        }

        bounds = Rect.Empty;
        return false;
    }

    private static bool TryGetBoundsRelativeToControl(FrameworkElement element, Control control, out Rect bounds)
    {
        bounds = Rect.Empty;
        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
            return false;

        try
        {
            var transform = element.TransformToAncestor(control);
            var topLeft = transform.Transform(new Point(0, 0));
            bounds = new Rect(topLeft, new Size(element.ActualWidth, element.ActualHeight));
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static Rect InsetBounds(Rect bounds, Thickness border, Thickness padding, double trailingReservedSpace)
    {
        var left = bounds.Left + border.Left + padding.Left;
        var top = bounds.Top + border.Top + padding.Top;
        var width = bounds.Width - border.Left - border.Right - padding.Left - padding.Right - trailingReservedSpace;
        var height = bounds.Height - border.Top - border.Bottom - padding.Top - padding.Bottom;

        return new Rect(left, top, Math.Max(0, width), Math.Max(0, height));
    }

    private static Rect ClampBounds(Rect bounds, Size finalSize)
    {
        var left = Math.Max(0, Math.Round(bounds.Left));
        var top = Math.Max(0, Math.Round(bounds.Top));
        var width = Math.Max(0, Math.Min(Math.Round(bounds.Width), finalSize.Width - left));
        var height = Math.Max(0, Math.Min(Math.Round(bounds.Height), finalSize.Height - top));

        return new Rect(left, top, width, height);
    }

    private static TextAlignment GetTextAlignment(Control control)
    {
        switch (control)
        {
            case TextBox textBox:
                return textBox.TextAlignment;
            case ComboBox comboBox when comboBox.IsEditable &&
                                       comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox editableTextBox:
                return editableTextBox.TextAlignment;
            case DatePicker datePicker when datePicker.Template?.FindName("PART_TextBox", datePicker) is DatePickerTextBox datePickerTextBox:
                return datePickerTextBox.TextAlignment;
            default:
                return TextAlignment.Left;
        }
    }

    private static void RemoveAdorner(Control control, AdornerLayer? layer = null)
    {
        layer ??= AdornerLayer.GetAdornerLayer(control);
        if (layer is null)
            return;

        var adorners = layer.GetAdorners(control);
        if (adorners is null)
            return;

        foreach (var adorner in adorners)
        {
            if (adorner is WatermarkAdorner)
                layer.Remove(adorner);
        }
    }

    private sealed class WatermarkAdorner : Adorner
    {
        private readonly Control _adornedControl;
        private readonly Brush _foreground;
        private readonly string _text;
        private readonly Typeface _typeface;
        private readonly TextAlignment _textAlignment;
        private readonly bool _isTopAligned;
        private readonly double _trailingReservedSpace;

        public WatermarkAdorner(
            Control adornedElement,
            string text,
            bool isTopAligned,
            double trailingReservedSpace) : base(adornedElement)
        {
            _adornedControl = adornedElement;
            _isTopAligned = isTopAligned;
            _trailingReservedSpace = trailingReservedSpace;
            IsHitTestVisible = false;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            _text = text;
            _textAlignment = GetTextAlignment(adornedElement);
            _foreground = adornedElement.TryFindResource("FluentTextTertiary") as Brush
                          ?? new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
            _typeface = new Typeface(
                adornedElement.FontFamily,
                adornedElement.FontStyle,
                adornedElement.FontWeight,
                adornedElement.FontStretch);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var contentBounds = GetContentBounds(_adornedControl, RenderSize, _trailingReservedSpace);
            if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
                return;

            var formattedText = new FormattedText(
                _text,
                CultureInfo.CurrentUICulture,
                _adornedControl.FlowDirection,
                _typeface,
                _adornedControl.FontSize,
                _foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            {
                MaxTextWidth = contentBounds.Width,
                MaxTextHeight = contentBounds.Height,
                TextAlignment = _textAlignment,
                Trimming = TextTrimming.CharacterEllipsis
            };

            var y = _isTopAligned
                ? contentBounds.Top
                : contentBounds.Top + Math.Max(0, Math.Floor((contentBounds.Height - formattedText.Height) / 2));

            drawingContext.DrawText(formattedText, new Point(contentBounds.Left, y));
        }
    }
}
