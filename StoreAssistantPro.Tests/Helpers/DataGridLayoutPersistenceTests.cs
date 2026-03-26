using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridLayoutPersistenceTests
{
    [Fact]
    public void CaptureLayout_Should_Record_Header_Order_Visibility_And_Width()
    {
        var invoice = new DataGridTextColumn { Header = "Invoice", DisplayIndex = 1, Width = new DataGridLength(160) };
        var total = new DataGridTextColumn { Header = "Total", DisplayIndex = 0, Visibility = Visibility.Collapsed, Width = new DataGridLength(96) };

        var layout = DataGridLayoutPersistence.CaptureLayout([invoice, total]);

        Assert.Equal(["Total", "Invoice"], layout.Columns.Select(column => column.Header));
        Assert.False(layout.Columns[0].IsVisible);
        Assert.Equal(96, layout.Columns[0].Width);
    }

    [Fact]
    public void ApplyLayout_Should_Update_Columns_By_Header()
    {
        var invoice = new DataGridTextColumn { Header = "Invoice", DisplayIndex = 0, Width = new DataGridLength(120) };
        var total = new DataGridTextColumn { Header = "Total", DisplayIndex = 1, Width = new DataGridLength(120) };
        var layout = new DataGridLayoutState
        {
            Columns =
            [
                new DataGridColumnLayoutState { Header = "Total", DisplayIndex = 0, IsVisible = false, Width = 96 },
                new DataGridColumnLayoutState { Header = "Invoice", DisplayIndex = 1, IsVisible = true, Width = 180 }
            ]
        };

        DataGridLayoutPersistence.ApplyLayout([invoice, total], layout);

        Assert.Equal(0, total.DisplayIndex);
        Assert.Equal(1, invoice.DisplayIndex);
        Assert.Equal(Visibility.Collapsed, total.Visibility);
        Assert.Equal(180, invoice.Width.Value);
    }
}
