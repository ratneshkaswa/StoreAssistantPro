using System.Windows.Controls;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class PasswordRevealTests
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
    public void PasswordChanges_UpdateHasRevealText()
    {
        RunOnSta(() =>
        {
            var passwordBox = new PasswordBox();

            PasswordReveal.SetIsEnabled(passwordBox, true);
            passwordBox.Password = "2468";

            Assert.True(PasswordReveal.GetHasRevealText(passwordBox));

            passwordBox.Password = string.Empty;

            Assert.False(PasswordReveal.GetHasRevealText(passwordBox));
        });
    }

    [Fact]
    public void RevealActive_ShowsAndClearsTransientPlaintext()
    {
        RunOnSta(() =>
        {
            var passwordBox = new PasswordBox();

            PasswordReveal.SetIsEnabled(passwordBox, true);
            passwordBox.Password = "1234";
            PasswordReveal.SetIsRevealActive(passwordBox, true);

            Assert.Equal("1234", PasswordReveal.GetRevealText(passwordBox));

            passwordBox.Password = "5678";

            Assert.Equal("5678", PasswordReveal.GetRevealText(passwordBox));

            PasswordReveal.SetIsRevealActive(passwordBox, false);

            Assert.Equal(string.Empty, PasswordReveal.GetRevealText(passwordBox));
        });
    }
}
