using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

public static partial class NumericInput
{
    public static readonly DependencyProperty ScopeProperty =
        DependencyProperty.RegisterAttached(
            "Scope", typeof(InputScopeNameValue), typeof(NumericInput),
            new PropertyMetadata(InputScopeNameValue.Default, OnScopeChanged));

    public static readonly DependencyProperty IsDecimalOnlyProperty =
        DependencyProperty.RegisterAttached(
            "IsDecimalOnly", typeof(bool), typeof(NumericInput),
            new PropertyMetadata(false, OnIsDecimalOnlyChanged));

    public static readonly DependencyProperty IsIntegerOnlyProperty =
        DependencyProperty.RegisterAttached(
            "IsIntegerOnly", typeof(bool), typeof(NumericInput),
            new PropertyMetadata(false, OnIsIntegerOnlyChanged));

    public static InputScopeNameValue GetScope(DependencyObject obj) =>
        (InputScopeNameValue)obj.GetValue(ScopeProperty);

    public static void SetScope(DependencyObject obj, InputScopeNameValue value) =>
        obj.SetValue(ScopeProperty, value);

    public static bool GetIsDecimalOnly(DependencyObject obj) => (bool)obj.GetValue(IsDecimalOnlyProperty);
    public static void SetIsDecimalOnly(DependencyObject obj, bool value) => obj.SetValue(IsDecimalOnlyProperty, value);

    public static bool GetIsIntegerOnly(DependencyObject obj) => (bool)obj.GetValue(IsIntegerOnlyProperty);
    public static void SetIsIntegerOnly(DependencyObject obj, bool value) => obj.SetValue(IsIntegerOnlyProperty, value);

    private static void OnIsDecimalOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += OnDecimalPreviewTextInput;
                DataObject.AddPastingHandler(textBox, OnDecimalPaste);
            }
            else
            {
                textBox.PreviewTextInput -= OnDecimalPreviewTextInput;
                DataObject.RemovePastingHandler(textBox, OnDecimalPaste);
            }

            ApplyInputScope(textBox);
        }
    }

    private static void OnIsIntegerOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += OnIntegerPreviewTextInput;
                DataObject.AddPastingHandler(textBox, OnIntegerPaste);
            }
            else
            {
                textBox.PreviewTextInput -= OnIntegerPreviewTextInput;
                DataObject.RemovePastingHandler(textBox, OnIntegerPaste);
            }

            ApplyInputScope(textBox);
        }
    }

    private static void OnScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
            ApplyInputScope(textBox);
    }

    private static void OnDecimalPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        var fullText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
        e.Handled = !DecimalPattern().IsMatch(fullText);
    }

    private static void OnIntegerPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IntegerPattern().IsMatch(e.Text);
    }

    private static void OnDecimalPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!DecimalPattern().IsMatch(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    private static void OnIntegerPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!IntegerPattern().IsMatch(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    [GeneratedRegex(@"^\d*\.?\d*$")]
    private static partial Regex DecimalPattern();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex IntegerPattern();

    internal static InputScope CreateInputScope(InputScopeNameValue scopeNameValue)
    {
        var inputScope = new InputScope();
        inputScope.Names.Add(new InputScopeName(scopeNameValue));
        return inputScope;
    }

    private static void ApplyInputScope(TextBox textBox)
    {
        var scope = GetScope(textBox);
        if (scope == InputScopeNameValue.Default)
        {
            scope = GetIsIntegerOnly(textBox)
                ? InputScopeNameValue.Digits
                : GetIsDecimalOnly(textBox)
                    ? InputScopeNameValue.Number
                    : InputScopeNameValue.Default;
        }

        textBox.InputScope = scope == InputScopeNameValue.Default
            ? null
            : CreateInputScope(scope);
    }
}
