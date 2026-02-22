using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class ResumeBillingDialogViewModelTests
{
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly DateTime _now = new(2025, 6, 15, 14, 0, 0, DateTimeKind.Utc);

    public ResumeBillingDialogViewModelTests()
    {
        _regional.Now.Returns(_now);
        _regional.FormatDateTime(Arg.Any<DateTime>())
            .Returns(ci => ci.Arg<DateTime>().ToString("dd-MM-yyyy hh:mm tt"));
    }

    private ResumeBillingDialogViewModel CreateSut(
        BillingSession? session = null,
        UserType userType = UserType.Admin) =>
        new(session ?? CreateSession(), userType, _regional);

    // ── Display properties ─────────────────────────────────────────

    [Fact]
    public void BillStartTime_FormattedFromCreatedTime()
    {
        var session = CreateSession(createdTime: new DateTime(2025, 6, 15, 9, 30, 0, DateTimeKind.Utc));

        var sut = CreateSut(session);

        Assert.Equal("15-06-2025 09:30 AM", sut.BillStartTime);
    }

    [Fact]
    public void UserDisplay_ShowsCurrentUserType()
    {
        var sut = CreateSut(userType: UserType.Manager);

        Assert.Equal("Manager", sut.UserDisplay);
    }

    [Theory]
    [InlineData("""{"sessionId":"00000000-0000-0000-0000-000000000000","items":[{"productId":1},{"productId":2}]}""", 2)]
    [InlineData("""{"items":[]}""", 0)]
    [InlineData("{}", 0)]
    [InlineData("invalid json", 0)]
    public void ItemCount_ExtractedFromSerializedData(string json, int expected)
    {
        var session = CreateSession(serializedData: json);

        var sut = CreateSut(session);

        Assert.Equal(expected, sut.ItemCount);
    }

    // ── Elapsed time formatting ────────────────────────────────────

    [Fact]
    public void ElapsedTime_MinutesAgo()
    {
        var session = CreateSession(lastUpdated: _now.AddMinutes(-15));

        var sut = CreateSut(session);

        Assert.Equal("15 min ago", sut.ElapsedTime);
    }

    [Fact]
    public void ElapsedTime_HoursAgo()
    {
        var session = CreateSession(lastUpdated: _now.AddHours(-3).AddMinutes(-25));

        var sut = CreateSut(session);

        Assert.Equal("3h 25m ago", sut.ElapsedTime);
    }

    [Fact]
    public void ElapsedTime_DaysAgo()
    {
        var session = CreateSession(lastUpdated: _now.AddDays(-2).AddHours(-5));

        var sut = CreateSut(session);

        Assert.Equal("2d 5h ago", sut.ElapsedTime);
    }

    [Fact]
    public void ElapsedTime_JustNow()
    {
        var session = CreateSession(lastUpdated: _now.AddSeconds(-10));

        var sut = CreateSut(session);

        Assert.Equal("just now", sut.ElapsedTime);
    }

    // ── Resume command ─────────────────────────────────────────────

    [Fact]
    public void ResumeCommand_SetsUserChoseResumeTrue()
    {
        var sut = CreateSut();
        bool? dialogResult = null;
        sut.CloseDialog = r => dialogResult = r;

        sut.ResumeCommand.Execute(null);

        Assert.True(sut.UserChoseResume);
        Assert.True(dialogResult);
    }

    // ── Discard command ────────────────────────────────────────────

    [Fact]
    public void DiscardCommand_SetsUserChoseResumeFalse()
    {
        var sut = CreateSut();
        bool? dialogResult = null;
        sut.CloseDialog = r => dialogResult = r;

        sut.DiscardCommand.Execute(null);

        Assert.False(sut.UserChoseResume);
        Assert.False(dialogResult);
    }

    // ── Default state ──────────────────────────────────────────────

    [Fact]
    public void DefaultState_UserChoseResumeIsFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.UserChoseResume);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private BillingSession CreateSession(
        DateTime? createdTime = null,
        DateTime? lastUpdated = null,
        string serializedData = """{"items":[{"productId":1}]}""") => new()
    {
        Id = 1,
        SessionId = Guid.NewGuid(),
        UserId = 1,
        IsActive = true,
        SerializedBillData = serializedData,
        CreatedTime = createdTime ?? new DateTime(2025, 6, 15, 9, 0, 0, DateTimeKind.Utc),
        LastUpdated = lastUpdated ?? new DateTime(2025, 6, 15, 9, 30, 0, DateTimeKind.Utc)
    };
}
