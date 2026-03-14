using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Views;

public partial class MasterPinDialog : BaseDialogWindow
{
    protected override double DialogWidth => 360;
    protected override double DialogHeight => 180;
    protected override double DialogMinWidth => 320;
    protected override double DialogMinHeight => 0;

    public string? EnteredPin { get; private set; }

    public MasterPinDialog(IWindowSizingService sizing, string message)
        : base(sizing)
    {
        InitializeComponent();
        SizeToContent = System.Windows.SizeToContent.Height;
        MinHeight = 0;
        PromptText.Text = message;
        Loaded += (_, _) => PinBox.Focus();
    }

    private void OnOkClick(object sender, System.Windows.RoutedEventArgs e)
    {
        EnteredPin = PinBox.Password;
        DialogResult = true;
    }
}
