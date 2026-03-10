using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views.SetupPages;

public partial class FirmProfilePage : Page
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
            if (!SetupViewModel.PhoneInputRegex().IsMatch(text))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    [GeneratedRegex(@"^[\d\s\+\-]$")]
    private static partial Regex PhoneCharRegex();
}
