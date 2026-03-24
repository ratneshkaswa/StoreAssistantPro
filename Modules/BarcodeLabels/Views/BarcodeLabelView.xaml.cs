using System.Windows.Controls;

namespace StoreAssistantPro.Modules.BarcodeLabels.Views;

public partial class BarcodeLabelView : UserControl
{
    public BarcodeLabelView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.BarcodeLabelViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
