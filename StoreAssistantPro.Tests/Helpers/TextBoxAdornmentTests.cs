using System.Windows.Controls;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class TextBoxAdornmentTests
{
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught is not null)
            throw new AggregateException(caught);
    }

    [Fact]
    public void PrefixAndSuffixText_UpdateVisibilityFlags()
    {
        RunOnSta(() =>
        {
            var textBox = new TextBox();

            TextBoxAdornment.SetPrefixText(textBox, "₹");
            TextBoxAdornment.SetSuffixText(textBox, "kg");

            Assert.Equal("₹", TextBoxAdornment.GetPrefixText(textBox));
            Assert.Equal("kg", TextBoxAdornment.GetSuffixText(textBox));
            Assert.True(TextBoxAdornment.GetHasPrefixText(textBox));
            Assert.True(TextBoxAdornment.GetHasSuffixText(textBox));
        });
    }

    [Fact]
    public void WhitespaceAdornmentText_ClearsVisibilityFlags()
    {
        RunOnSta(() =>
        {
            var textBox = new TextBox();

            TextBoxAdornment.SetPrefixText(textBox, " ");
            TextBoxAdornment.SetSuffixText(textBox, string.Empty);

            Assert.False(TextBoxAdornment.GetHasPrefixText(textBox));
            Assert.False(TextBoxAdornment.GetHasSuffixText(textBox));
        });
    }
}
