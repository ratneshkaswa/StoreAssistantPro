using System.Windows;
using StoreAssistantPro.ViewModels;

namespace StoreAssistantPro;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.RequestClose = Close;
        };
    }
}