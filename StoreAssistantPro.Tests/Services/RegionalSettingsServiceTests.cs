using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public class RegionalSettingsServiceTests
{
    [Fact]
    public void FormatCurrency_UsesUpdatedCurrencySymbol()
    {
        var sut = new RegionalSettingsService();

        sut.UpdateSettings("Rs.", "dd-MM-yyyy");
        var formatted = sut.FormatCurrency(1234.5m);

        Assert.Contains("Rs.", formatted);
        Assert.DoesNotContain("\u20B9", formatted);
    }

    [Fact]
    public void UpdateSettings_BlankValues_DoNotOverwriteExistingSettings()
    {
        var sut = new RegionalSettingsService();

        sut.UpdateSettings("Rs.", "yyyy-MM-dd");
        sut.UpdateSettings(" ", "");

        Assert.Equal("Rs.", sut.CurrencySymbol);
        Assert.Equal("yyyy-MM-dd", sut.DateFormat);
    }
}
