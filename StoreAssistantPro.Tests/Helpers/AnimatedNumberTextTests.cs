using System.Windows.Controls;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class AnimatedNumberTextTests
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
    public void CompactCurrencyValue_FormatsWithCurrencySymbol()
    {
        RunOnSta(() =>
        {
            var textBlock = new TextBlock();

            AnimatedNumberText.SetFormatMode(textBlock, AnimatedNumberFormatMode.CompactCurrency);
            AnimatedNumberText.SetCurrencySymbol(textBlock, "₹");
            AnimatedNumberText.SetValue(textBlock, 125000d);

            Assert.Equal("₹1.2L", textBlock.Text);
        });
    }

    [Fact]
    public void CompactNumberValue_FormatsWithIndianSuffixes()
    {
        RunOnSta(() =>
        {
            var textBlock = new TextBlock();

            AnimatedNumberText.SetFormatMode(textBlock, AnimatedNumberFormatMode.CompactNumber);
            AnimatedNumberText.SetValue(textBlock, 45000d);

            Assert.Equal("45K", textBlock.Text);
        });
    }

    [Fact]
    public void ChangingCurrencySymbol_ReformatsExistingAnimatedValue()
    {
        RunOnSta(() =>
        {
            var textBlock = new TextBlock();

            AnimatedNumberText.SetFormatMode(textBlock, AnimatedNumberFormatMode.CompactCurrency);
            AnimatedNumberText.SetCurrencySymbol(textBlock, "₹");
            AnimatedNumberText.SetValue(textBlock, 58000d);

            AnimatedNumberText.SetCurrencySymbol(textBlock, "$");

            Assert.Equal("$58K", textBlock.Text);
        });
    }
}
