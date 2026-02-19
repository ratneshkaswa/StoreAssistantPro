using System.Windows;
using StoreAssistantPro.ViewModels;

namespace StoreAssistantPro.Views;

public partial class PinLoginWindow : Window
{
    public PinLoginWindow(PinLoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose = result => DialogResult = result;
    }
}
