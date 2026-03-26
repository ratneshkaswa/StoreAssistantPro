using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class InteractionHarnessTests
{
    [Fact]
    public void Click_Helper_Should_Invoke_Button_Command()
    {
        var clicked = false;

        WpfTestApplication.Run(() =>
        {
            var button = new Button
            {
                Command = new RelayCommand(() => clicked = true)
            };

            WpfInteractionHelper.Click(button);

            Assert.True(clicked);
        });
    }
}
