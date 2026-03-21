using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Windows 11 WinUI-style NumberBox with optional spin buttons.
/// Replaces raw TextBox for quantity/price fields with proper
/// numeric validation, increment/decrement, and min/max clamping.
/// <para>
/// Bind <see cref="Value"/> (double) to the ViewModel property.
/// </para>
/// <example>
/// <code>&lt;controls:NumberBox Value="{Binding Quantity}" Minimum="0" Maximum="99999"
///                       SpinButtonPlacement="Inline" SmallChange="1"/&gt;</code>
/// </example>
/// </summary>
public class NumberBox : Control
{
    private TextBox? _textBox;
    private Button? _upButton;
    private Button? _downButton;

    static NumberBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NumberBox), new FrameworkPropertyMetadata(typeof(NumberBox)));
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumberBox),
            new FrameworkPropertyMetadata(0.0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged, CoerceValue));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumberBox),
            new PropertyMetadata(double.MinValue));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumberBox),
            new PropertyMetadata(double.MaxValue));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty SmallChangeProperty =
        DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(NumberBox),
            new PropertyMetadata(1.0));

    public double SmallChange
    {
        get => (double)GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(NumberBox),
            new PropertyMetadata(string.Empty));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public static readonly DependencyProperty PrefixTextProperty =
        DependencyProperty.Register(nameof(PrefixText), typeof(string), typeof(NumberBox),
            new PropertyMetadata(string.Empty, OnAdornmentTextChanged));

    public string PrefixText
    {
        get => (string)GetValue(PrefixTextProperty);
        set => SetValue(PrefixTextProperty, value);
    }

    private static readonly DependencyPropertyKey HasPrefixTextPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(HasPrefixText), typeof(bool), typeof(NumberBox),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasPrefixTextProperty = HasPrefixTextPropertyKey.DependencyProperty;

    public bool HasPrefixText => (bool)GetValue(HasPrefixTextProperty);

    public static readonly DependencyProperty SuffixTextProperty =
        DependencyProperty.Register(nameof(SuffixText), typeof(string), typeof(NumberBox),
            new PropertyMetadata(string.Empty, OnAdornmentTextChanged));

    public string SuffixText
    {
        get => (string)GetValue(SuffixTextProperty);
        set => SetValue(SuffixTextProperty, value);
    }

    private static readonly DependencyPropertyKey HasSuffixTextPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(HasSuffixText), typeof(bool), typeof(NumberBox),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasSuffixTextProperty = HasSuffixTextPropertyKey.DependencyProperty;

    public bool HasSuffixText => (bool)GetValue(HasSuffixTextProperty);

    public static readonly DependencyProperty SpinButtonPlacementProperty =
        DependencyProperty.Register(nameof(SpinButtonPlacement), typeof(SpinButtonPlacement), typeof(NumberBox),
            new PropertyMetadata(SpinButtonPlacement.Hidden));

    public SpinButtonPlacement SpinButtonPlacement
    {
        get => (SpinButtonPlacement)GetValue(SpinButtonPlacementProperty);
        set => SetValue(SpinButtonPlacementProperty, value);
    }

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(NumberBox),
            new PropertyMetadata(0));

    public int DecimalPlaces
    {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_textBox is not null)
        {
            _textBox.PreviewTextInput -= OnPreviewTextInput;
            _textBox.LostFocus -= OnTextBoxLostFocus;
            _textBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
            DataObject.RemovePastingHandler(_textBox, OnPaste);
        }

        if (_upButton is not null)
            _upButton.Click -= OnUpButtonClick;

        if (_downButton is not null)
            _downButton.Click -= OnDownButtonClick;

        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        if (_textBox is not null)
        {
            _textBox.PreviewTextInput += OnPreviewTextInput;
            _textBox.LostFocus += OnTextBoxLostFocus;
            _textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
            DataObject.AddPastingHandler(_textBox, OnPaste);
            SyncTextFromValue();
        }

        _upButton = GetTemplateChild("PART_UpButton") as Button;
        if (_upButton is not null)
            _upButton.Click += OnUpButtonClick;

        _downButton = GetTemplateChild("PART_DownButton") as Button;
        if (_downButton is not null)
            _downButton.Click += OnDownButtonClick;
    }

    private void Increment() => Value += SmallChange;
    private void Decrement() => Value -= SmallChange;

    private void OnUpButtonClick(object sender, RoutedEventArgs e) => Increment();
    private void OnDownButtonClick(object sender, RoutedEventArgs e) => Decrement();

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Allow digits, minus (at start), and decimal point
        foreach (var c in e.Text)
        {
            if (char.IsDigit(c)) continue;
            if (c == '-' && _textBox?.CaretIndex == 0 && _textBox.Text.IndexOf('-') < 0) continue;
            if (c == '.' && DecimalPlaces > 0 && _textBox?.Text.IndexOf('.') < 0) continue;
            e.Handled = true;
            return;
        }
    }

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(typeof(string)) is string text &&
            !double.TryParse(text, CultureInfo.InvariantCulture, out _))
        {
            e.CancelCommand();
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e) => CommitText();

    private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                Increment();
                e.Handled = true;
                break;
            case Key.Down:
                Decrement();
                e.Handled = true;
                break;
            case Key.Enter:
                CommitText();
                e.Handled = true;
                break;
        }
    }

    private void CommitText()
    {
        if (_textBox is null) return;
        if (double.TryParse(_textBox.Text, CultureInfo.InvariantCulture, out var parsed))
            Value = parsed;
        else
            SyncTextFromValue();
    }

    private void SyncTextFromValue()
    {
        if (_textBox is null) return;
        _textBox.Text = DecimalPlaces > 0
            ? Value.ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture)
            : ((int)Value).ToString(CultureInfo.InvariantCulture);
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        if (d is not NumberBox nb || baseValue is not double val) return baseValue;
        return Math.Clamp(val, nb.Minimum, nb.Maximum);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox nb)
            nb.SyncTextFromValue();
    }

    private static void OnAdornmentTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox nb)
            nb.UpdateAdornmentState();
    }

    private void UpdateAdornmentState()
    {
        SetValue(HasPrefixTextPropertyKey, !string.IsNullOrWhiteSpace(PrefixText));
        SetValue(HasSuffixTextPropertyKey, !string.IsNullOrWhiteSpace(SuffixText));
    }
}

public enum SpinButtonPlacement
{
    Hidden,
    Inline
}
