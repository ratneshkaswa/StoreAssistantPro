using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class PinHasherTests
{
    [Fact]
    public void Hash_ProducesSaltDotHashFormat()
    {
        var hash = PinHasher.Hash("1234");

        Assert.Contains('.', hash);
        var parts = hash.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.False(string.IsNullOrWhiteSpace(parts[0]));
        Assert.False(string.IsNullOrWhiteSpace(parts[1]));
    }

    [Fact]
    public void Verify_CorrectPin_ReturnsTrue()
    {
        var hash = PinHasher.Hash("5678");

        Assert.True(PinHasher.Verify("5678", hash));
    }

    [Fact]
    public void Verify_WrongPin_ReturnsFalse()
    {
        var hash = PinHasher.Hash("5678");

        Assert.False(PinHasher.Verify("0000", hash));
    }

    [Fact]
    public void Hash_SamePinTwice_ProducesDifferentHashes()
    {
        var hash1 = PinHasher.Hash("1234");
        var hash2 = PinHasher.Hash("1234");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse()
    {
        Assert.False(PinHasher.Verify("1234", "not-a-valid-hash"));
    }
}
