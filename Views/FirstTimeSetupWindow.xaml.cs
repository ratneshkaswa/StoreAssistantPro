using System.Windows;
using StoreAssistantPro.ViewModels;

namespace StoreAssistantPro.Views;

public partial class FirstTimeSetupWindow : Window
{
    public FirstTimeSetupWindow(FirstTimeSetupViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose = result => DialogResult = result;
    }
}
