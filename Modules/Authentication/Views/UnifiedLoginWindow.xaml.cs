using System.Windows;
using System.Windows.Input;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views;

public partial class UnifiedLoginWindow : Window
{
    public UnifiedLoginWindow(UnifiedLoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose = result => DialogResult = result;
        vm.Initialize();

        PreviewKeyDown += OnPreviewKeyDown;
    }

    /// <summary>
    /// Routes physical keyboard input to PIN pad commands so the on-screen
    /// keypad and the physical keyboard work identically — no code-behind
    /// logic, just command delegation.
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not UnifiedLoginViewModel vm)
            return;

        var digit = e.Key switch
        {
            Key.D0 or Key.NumPad0 => "0",
            Key.D1 or Key.NumPad1 => "1",
            Key.D2 or Key.NumPad2 => "2",
            Key.D3 or Key.NumPad3 => "3",
            Key.D4 or Key.NumPad4 => "4",
            Key.D5 or Key.NumPad5 => "5",
            Key.D6 or Key.NumPad6 => "6",
            Key.D7 or Key.NumPad7 => "7",
            Key.D8 or Key.NumPad8 => "8",
            Key.D9 or Key.NumPad9 => "9",
            _ => null
        };

        if (digit is not null)
        {
            vm.PinPad.AddDigitCommand.Execute(digit);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Back:
                vm.PinPad.BackspaceCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete:
                vm.PinPad.ClearCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                if (vm.LoginCommand.CanExecute(null))
                    vm.LoginCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
