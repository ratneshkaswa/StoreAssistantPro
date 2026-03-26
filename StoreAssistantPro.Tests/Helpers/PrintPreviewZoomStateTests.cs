using StoreAssistantPro.Core.Printing;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("UserPreferences")]
public sealed class PrintPreviewZoomStateTests : IDisposable
{
    public void Dispose() => PrintPreviewZoomState.Clear();

    [Fact]
    public void Get_Should_Return_Default_Zoom_For_Unseen_Key()
    {
        var zoom = PrintPreviewZoomState.Get("Sales report");

        Assert.Equal(100, zoom);
    }

    [Fact]
    public void Set_Should_Remember_Zoom_Per_Key()
    {
        PrintPreviewZoomState.Set("Billing", 140);
        PrintPreviewZoomState.Set("Inventory", 90);

        Assert.Equal(140, PrintPreviewZoomState.Get("Billing"));
        Assert.Equal(90, PrintPreviewZoomState.Get("Inventory"));
    }

    [Theory]
    [InlineData(20, 80)]
    [InlineData(100, 100)]
    [InlineData(260, 200)]
    public void Set_Should_Clamp_Zoom_To_PrintPreview_Range(double input, double expected)
    {
        PrintPreviewZoomState.Set("Barcode labels", input);

        Assert.Equal(expected, PrintPreviewZoomState.Get("Barcode labels"));
    }
}
