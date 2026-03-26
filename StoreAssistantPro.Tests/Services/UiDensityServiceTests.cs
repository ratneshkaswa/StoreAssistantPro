using StoreAssistantPro.Tests.Helpers;
using System.Windows;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

[Collection("UserPreferences")]
public sealed class UiDensityServiceTests : IDisposable
{
    public UiDensityServiceTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public void SetCompactMode_Should_Update_Shared_Density_Resources()
    {
        WpfTestApplication.Run(() =>
        {
            var app = Application.Current ?? throw new InvalidOperationException("WPF app not initialized.");
            app.Resources["NormalDensityDataGridRowHeight"] = 32d;
            app.Resources["CompactDensityDataGridRowHeight"] = 28d;
            app.Resources["NormalDensityDataGridCellPadding"] = new Thickness(12, 8, 12, 8);
            app.Resources["CompactDensityDataGridCellPadding"] = new Thickness(12, 4, 12, 4);
            app.Resources["NormalDensityDataGridHeaderPadding"] = new Thickness(12, 8, 12, 8);
            app.Resources["CompactDensityDataGridHeaderPadding"] = new Thickness(12, 4, 12, 4);

            var sut = new UiDensityService();

            sut.SetCompactMode(true);

            Assert.True(sut.IsCompactModeEnabled);
            Assert.Equal(28d, Assert.IsType<double>(app.Resources["AppDataGridRowHeight"]));
            Assert.Equal(new Thickness(12, 4, 12, 4), Assert.IsType<Thickness>(app.Resources["AppDataGridCellPadding"]));
            Assert.Equal(new Thickness(12, 4, 12, 4), Assert.IsType<Thickness>(app.Resources["AppDataGridHeaderPadding"]));
        });
    }
}
