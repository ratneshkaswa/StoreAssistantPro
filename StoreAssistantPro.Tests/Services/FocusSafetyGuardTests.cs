using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

/// <summary>
/// Tests for <see cref="FocusSafetyGuard"/> — the three-rule safety
/// system that prevents predictive focus from disrupting user intent.
/// </summary>
public class FocusSafetyGuardTests
{
    private readonly IPredictiveFocusService _focusService = Substitute.For<IPredictiveFocusService>();

    private FocusSafetyGuard CreateSut()
    {
        _focusService.IsUserInputActive.Returns(false);
        return new FocusSafetyGuard(_focusService);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Initial state
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void InitialState_AllGuardsInactive()
    {
        var sut = CreateSut();

        Assert.False(sut.IsDialogOpen);
        Assert.False(sut.IsClickCooldownActive);
    }

    [Fact]
    public void InitialState_CanExecuteHint()
    {
        var sut = CreateSut();
        var hint = FocusHint.FirstInput("test");

        Assert.True(sut.CanExecute(hint));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Rule 1: Typing guard — never steal focus while user typing
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void TypingActive_BlocksFirstInputHint()
    {
        var sut = CreateSut();
        _focusService.IsUserInputActive.Returns(true);

        var hint = FocusHint.FirstInput("PageNavigated");

        Assert.False(sut.CanExecute(hint));
    }

    [Fact]
    public void TypingActive_BlocksNamedHint()
    {
        var sut = CreateSut();
        _focusService.IsUserInputActive.Returns(true);

        var hint = FocusHint.Named("SearchBox", "ModeChanged");

        Assert.False(sut.CanExecute(hint));
    }

    [Fact]
    public void TypingActive_AllowsPreserveHint()
    {
        var sut = CreateSut();
        _focusService.IsUserInputActive.Returns(true);

        var hint = FocusHint.Preserve("UserTyping");

        Assert.True(sut.CanExecute(hint));
    }

    [Fact]
    public void TypingInactive_AllowsHint()
    {
        _focusService.IsUserInputActive.Returns(false);
        var sut = CreateSut();

        Assert.True(sut.CanExecute(FocusHint.FirstInput("test")));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Rule 2: Dialog guard — ignore focus changes during dialogs
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void DialogOpen_BlocksHint()
    {
        var sut = CreateSut();
        sut.SignalDialogOpened();

        Assert.True(sut.IsDialogOpen);
        Assert.False(sut.CanExecute(FocusHint.FirstInput("test")));
    }

    [Fact]
    public void DialogOpen_BlocksNamedHint()
    {
        var sut = CreateSut();
        sut.SignalDialogOpened();

        Assert.False(sut.CanExecute(FocusHint.Named("Box", "test")));
    }

    [Fact]
    public void DialogOpen_AllowsPreserveHint()
    {
        var sut = CreateSut();
        sut.SignalDialogOpened();

        Assert.True(sut.CanExecute(FocusHint.Preserve("test")));
    }

    [Fact]
    public void DialogClosed_AllowsHint()
    {
        var sut = CreateSut();
        sut.SignalDialogOpened();
        sut.SignalDialogClosed();

        Assert.False(sut.IsDialogOpen);
        Assert.True(sut.CanExecute(FocusHint.FirstInput("test")));
    }

    [Fact]
    public void NestedDialogs_RequireAllClosed()
    {
        var sut = CreateSut();
        sut.SignalDialogOpened();
        sut.SignalDialogOpened();

        // Close one — still blocked
        sut.SignalDialogClosed();
        Assert.True(sut.IsDialogOpen);
        Assert.False(sut.CanExecute(FocusHint.FirstInput("test")));

        // Close the second — unblocked
        sut.SignalDialogClosed();
        Assert.False(sut.IsDialogOpen);
        Assert.True(sut.CanExecute(FocusHint.FirstInput("test")));
    }

    [Fact]
    public void DialogClosed_WhenNoneOpen_DoesNotGoNegative()
    {
        var sut = CreateSut();

        // Close without open — no crash, stays at zero
        sut.SignalDialogClosed();

        Assert.False(sut.IsDialogOpen);
        Assert.True(sut.CanExecute(FocusHint.FirstInput("test")));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Rule 3: Click guard — respect manual user clicks
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void UserClick_ActivatesCooldown()
    {
        var sut = CreateSut();
        sut.SignalUserClick();

        Assert.True(sut.IsClickCooldownActive);
    }

    [Fact]
    public void UserClick_BlocksHintDuringCooldown()
    {
        var sut = CreateSut();
        sut.SignalUserClick();

        Assert.False(sut.CanExecute(FocusHint.FirstInput("PageNavigated")));
        Assert.False(sut.CanExecute(FocusHint.Named("Box", "test")));
    }

    [Fact]
    public void UserClick_AllowsPreserveDuringCooldown()
    {
        var sut = CreateSut();
        sut.SignalUserClick();

        Assert.True(sut.CanExecute(FocusHint.Preserve("test")));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Combined rules — all must pass
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void TypingAndDialog_BothBlock()
    {
        var sut = CreateSut();
        _focusService.IsUserInputActive.Returns(true);
        sut.SignalDialogOpened();

        // Blocked by typing (first check) even though dialog also blocks
        Assert.False(sut.CanExecute(FocusHint.FirstInput("test")));
    }

    [Fact]
    public void DialogAndClick_BothBlock()
    {
        var sut = CreateSut();
        sut.SignalDialogOpened();
        sut.SignalUserClick();

        Assert.False(sut.CanExecute(FocusHint.Named("Box", "test")));

        // Close dialog — still blocked by click cooldown
        sut.SignalDialogClosed();
        Assert.False(sut.CanExecute(FocusHint.Named("Box", "test")));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Null hint guard
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NullHint_Throws()
    {
        var sut = CreateSut();

        Assert.Throws<ArgumentNullException>(() => sut.CanExecute(null!));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Cooldown constant is reasonable
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ClickCooldown_IsReasonableDuration()
    {
        // 400 ms — long enough to prevent immediate hint execution
        // after a click, short enough to not feel sluggish
        Assert.Equal(400, FocusSafetyGuard.ClickCooldownMs);
    }
}
