using StoreAssistantPro.Modules.SystemSettings.Services;

namespace StoreAssistantPro.Tests.Services;

/// <summary>
/// Tests for <see cref="DatabaseIntegrityResult"/> record.
/// Feature #335 — Database integrity check.
/// </summary>
public class DatabaseIntegrityResultTests
{
    [Fact]
    public void Healthy_Result()
    {
        var result = new DatabaseIntegrityResult(true, "ok", 1_048_576);

        Assert.True(result.IsHealthy);
        Assert.Equal("ok", result.Details);
        Assert.Equal(1_048_576, result.DatabaseSizeBytes);
    }

    [Fact]
    public void Unhealthy_Result()
    {
        var result = new DatabaseIntegrityResult(false, "row 5 missing from index", 2_097_152);

        Assert.False(result.IsHealthy);
        Assert.Contains("missing", result.Details);
    }

    [Fact]
    public void SizeBytes_FormatsToMB()
    {
        var result = new DatabaseIntegrityResult(true, "ok", 5_242_880);

        var mb = result.DatabaseSizeBytes / (1024.0 * 1024.0);
        Assert.Equal(5.0, mb, 1);
    }

    [Fact]
    public void Record_Equality()
    {
        var a = new DatabaseIntegrityResult(true, "ok", 100);
        var b = new DatabaseIntegrityResult(true, "ok", 100);

        Assert.Equal(a, b);
    }
}
