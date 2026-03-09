using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views.SetupPages;

public partial class SecuritySettingsPage : Page
{
    public SecuritySettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PasswordBox[] pinBoxes = [AdminPinBox, AdminPinConfirmBox, ManagerPinBox, ManagerPinConfirmBox,
                                   UserPinBox, UserPinConfirmBox, MasterPinBox, MasterPinConfirmBox];
        foreach (var box in pinBoxes)
        {
            box.PreviewTextInput += OnPreviewNumericOnly;
            DataObject.AddPastingHandler(box, OnPastingNumericOnly);
        }
    }

    private void OnPinChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SetupViewModel vm) return;

        if (sender == AdminPinBox)
            vm.AdminPin = AdminPinBox.Password;
        else if (sender == AdminPinConfirmBox)
            vm.AdminPinConfirm = AdminPinConfirmBox.Password;
        else if (sender == ManagerPinBox)
            vm.ManagerPin = ManagerPinBox.Password;
        else if (sender == ManagerPinConfirmBox)
            vm.ManagerPinConfirm = ManagerPinConfirmBox.Password;
        else if (sender == UserPinBox)
            vm.UserPin = UserPinBox.Password;
        else if (sender == UserPinConfirmBox)
            vm.UserPinConfirm = UserPinConfirmBox.Password;
        else if (sender == MasterPinBox)
            vm.MasterPin = MasterPinBox.Password;
        else if (sender == MasterPinConfirmBox)
            vm.MasterPinConfirm = MasterPinConfirmBox.Password;

        if (sender is PasswordBox pb && pb.Password.Length == pb.MaxLength)
            AdvancePinFocus(pb);
    }

    private void AdvancePinFocus(PasswordBox current)
    {
        PasswordBox? next = current == MasterPinBox ? MasterPinConfirmBox
            : current == MasterPinConfirmBox ? AdminPinBox
            : current == AdminPinBox ? AdminPinConfirmBox
            : current == AdminPinConfirmBox ? ManagerPinBox
            : current == ManagerPinBox ? ManagerPinConfirmBox
            : current == ManagerPinConfirmBox ? UserPinBox
            : current == UserPinBox ? UserPinConfirmBox
            : null;

        next?.Focus();
    }

    private static void OnPreviewNumericOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !DigitsOnlyRegex().IsMatch(e.Text);
    }

    private static void OnPastingNumericOnly(object sender, DataObjectPastingEventArgs e)
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

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
