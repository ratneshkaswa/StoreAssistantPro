using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Modules.Authentication.Views.SetupPages;

public partial class TaxLegalPage : Page
{
    private bool _handlersAttached;

    public TaxLegalPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_handlersAttached) return;
        _handlersAttached = true;

        DataObject.AddPastingHandler(GstinBox, OnPastingUpperCase);
        DataObject.AddPastingHandler(PanBox, OnPastingUpperCase);
    }

    private static void OnPastingUpperCase(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            var upper = text.ToUpperInvariant();
            if (upper != text)
            {
                e.CancelCommand();
                if (sender is TextBox tb)
                {
                    var selStart = tb.SelectionStart;
                    var selLength = tb.SelectionLength;
                    var remaining = tb.Text.Remove(selStart, selLength);
                    var available = tb.MaxLength > 0 ? tb.MaxLength - remaining.Length : int.MaxValue;
                    var insert = upper[..Math.Min(upper.Length, available)];
                    if (insert.Length > 0)
                    {
                        tb.Text = remaining.Insert(selStart, insert);
                        tb.CaretIndex = selStart + insert.Length;
                    }
                }
            }
        }
    }
}
