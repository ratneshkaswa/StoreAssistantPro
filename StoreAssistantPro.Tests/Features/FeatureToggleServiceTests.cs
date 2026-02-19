using StoreAssistantPro.Core.Features;

namespace StoreAssistantPro.Tests.Features;

public class FeatureToggleServiceTests
{
    [Fact]
    public void IsEnabled_UnknownFeature_ReturnsTrue()
    {
        var sut = new FeatureToggleService();

        Assert.True(sut.IsEnabled("UnknownFeature"));
    }

    [Fact]
    public void IsEnabled_ExplicitlyEnabled_ReturnsTrue()
    {
        var sut = new FeatureToggleService();
        sut.Load(new Dictionary<string, bool> { ["Products"] = true });

        Assert.True(sut.IsEnabled("Products"));
    }

    [Fact]
    public void IsEnabled_ExplicitlyDisabled_ReturnsFalse()
    {
        var sut = new FeatureToggleService();
        sut.Load(new Dictionary<string, bool> { ["Billing"] = false });

        Assert.False(sut.IsEnabled("Billing"));
    }

    [Fact]
    public void IsEnabled_CaseInsensitive()
    {
        var sut = new FeatureToggleService();
        sut.Load(new Dictionary<string, bool> { ["billing"] = false });

        Assert.False(sut.IsEnabled("Billing"));
        Assert.False(sut.IsEnabled("BILLING"));
        Assert.False(sut.IsEnabled("billing"));
    }

    [Fact]
    public void Load_RaisesPropertyChanged()
    {
        var sut = new FeatureToggleService();
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        sut.Load(new Dictionary<string, bool> { ["Products"] = true });

        Assert.True(raised);
    }

    [Fact]
    public void AllFlags_ReturnsLoadedDictionary()
    {
        var sut = new FeatureToggleService();
        var flags = new Dictionary<string, bool>
        {
            ["Products"] = true,
            ["Billing"] = false,
            ["Reports"] = false
        };

        sut.Load(flags);

        Assert.Equal(3, sut.AllFlags.Count);
        Assert.True(sut.AllFlags["Products"]);
        Assert.False(sut.AllFlags["Billing"]);
    }

    [Fact]
    public void Load_OverwritesPreviousFlags()
    {
        var sut = new FeatureToggleService();
        sut.Load(new Dictionary<string, bool> { ["Billing"] = false });

        Assert.False(sut.IsEnabled("Billing"));

        sut.Load(new Dictionary<string, bool> { ["Billing"] = true });

        Assert.True(sut.IsEnabled("Billing"));
    }
}
