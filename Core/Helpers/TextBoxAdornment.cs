using System.Windows;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Adds lightweight prefix/suffix labels to shared TextBox chrome.
/// </summary>
public static class TextBoxAdornment
{
    public static readonly DependencyProperty PrefixTextProperty =
        DependencyProperty.RegisterAttached(
            "PrefixText",
            typeof(string),
            typeof(TextBoxAdornment),
            new PropertyMetadata(null, OnPrefixTextChanged));

    public static readonly DependencyProperty SuffixTextProperty =
        DependencyProperty.RegisterAttached(
            "SuffixText",
            typeof(string),
            typeof(TextBoxAdornment),
            new PropertyMetadata(null, OnSuffixTextChanged));

    private static readonly DependencyPropertyKey HasPrefixTextPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "HasPrefixText",
            typeof(bool),
            typeof(TextBoxAdornment),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyKey HasSuffixTextPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "HasSuffixText",
            typeof(bool),
            typeof(TextBoxAdornment),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasPrefixTextProperty = HasPrefixTextPropertyKey.DependencyProperty;
    public static readonly DependencyProperty HasSuffixTextProperty = HasSuffixTextPropertyKey.DependencyProperty;

    public static string? GetPrefixText(DependencyObject obj) => (string?)obj.GetValue(PrefixTextProperty);

    public static void SetPrefixText(DependencyObject obj, string? value) => obj.SetValue(PrefixTextProperty, value);

    public static string? GetSuffixText(DependencyObject obj) => (string?)obj.GetValue(SuffixTextProperty);

    public static void SetSuffixText(DependencyObject obj, string? value) => obj.SetValue(SuffixTextProperty, value);

    public static bool GetHasPrefixText(DependencyObject obj) => (bool)obj.GetValue(HasPrefixTextProperty);

    public static bool GetHasSuffixText(DependencyObject obj) => (bool)obj.GetValue(HasSuffixTextProperty);

    private static void OnPrefixTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        d.SetValue(HasPrefixTextPropertyKey, HasAdornmentText(e.NewValue as string));

    private static void OnSuffixTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        d.SetValue(HasSuffixTextPropertyKey, HasAdornmentText(e.NewValue as string));

    private static bool HasAdornmentText(string? text) => !string.IsNullOrWhiteSpace(text);
}
