using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridColumnChooserTests
{
    [Fact]
    public void GetHeaderText_Should_Trim_String_Headers()
    {
        Assert.Equal("Category", DataGridColumnChooser.GetHeaderText("  Category  "));
    }

    [Fact]
    public void GetChoices_Should_Skip_Blank_Headers_And_Preserve_Visibility()
    {
        var product = new DataGridTextColumn { Header = "Product" };
        var hiddenCost = new DataGridTextColumn
        {
            Header = "Cost",
            Visibility = Visibility.Collapsed
        };
        var blank = new DataGridTextColumn { Header = string.Empty };

        var choices = DataGridColumnChooser.GetChoices([product, hiddenCost, blank]);

        Assert.Collection(
            choices,
            choice =>
            {
                Assert.Equal("Product", choice.Header);
                Assert.True(choice.IsVisible);
            },
            choice =>
            {
                Assert.Equal("Cost", choice.Header);
                Assert.False(choice.IsVisible);
            });
    }
}
