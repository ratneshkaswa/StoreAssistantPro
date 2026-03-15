using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views.SetupPages;

public partial class FirmProfilePage : UserControl
{
    private bool _handlersAttached;

    public FirmProfilePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_handlersAttached) return;
        _handlersAttached = true;

        PhoneBox.PreviewTextInput += OnPreviewPhoneOnly;
        DataObject.AddPastingHandler(PhoneBox, OnPastingPhoneOnly);
        PhoneBox.TextChanged += OnPhoneTextChanged;

        PincodeBox.PreviewTextInput += OnPreviewPhoneOnly;
        DataObject.AddPastingHandler(PincodeBox, OnPastingPhoneOnly);
    }

    private static void OnPreviewPhoneOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !PhoneCharRegex().IsMatch(e.Text);
    }

    private static void OnPastingPhoneOnly(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!DigitsOnlyRegex().IsMatch(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    private void OnPhoneTextChanged(object sender, TextChangedEventArgs e)
    {
        var digitsOnly = NormalizePhoneDigits(PhoneBox.Text);

        if (!string.Equals(PhoneBox.Text, digitsOnly, StringComparison.Ordinal))
        {
            var caret = digitsOnly.Length;
            PhoneBox.Text = digitsOnly;
            PhoneBox.CaretIndex = caret;
        }
    }

    internal static string NormalizePhoneDigits(string? text)
    {
        var digitsOnly = NonDigitsRegex().Replace(text ?? string.Empty, string.Empty);
        return digitsOnly.Length > 10
            ? digitsOnly[..10]
            : digitsOnly;
    }

    private void OnToggleOptionalFieldsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupViewModel vm)
            vm.ShowOptionalFirmFields = !vm.ShowOptionalFirmFields;
    }

    [GeneratedRegex(@"^\d$")]
    private static partial Regex PhoneCharRegex();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();

    [GeneratedRegex(@"\D+")]
    private static partial Regex NonDigitsRegex();
}
