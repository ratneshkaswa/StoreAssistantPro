using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Offline;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Commands;

public class OfflinePipelineBehaviorTests
{
    // ══════════════════════════════════════════════════════════════
    //  Test command types
    // ══════════════════════════════════════════════════════════════

    /// <summary>Requires DB — blocked while offline.</summary>
    private sealed record OnlineOnlyCmd(string Name)
        : ICommandRequest<int>, IOnlineOnlyCommand;

    /// <summary>Has offline fallback — allowed while offline.</summary>
    private sealed record OfflineCapableCmd(string Data)
        : ICommandRequest<string>, IOfflineCapableCommand;

    /// <summary>No marker — always passes through.</summary>
    private sealed record UnmarkedCmd(int Value)
        : ICommandRequest<bool>;

    /// <summary>Both markers — IOnlineOnly takes precedence.</summary>
    private sealed record ConflictingCmd(string Name)
        : ICommandRequest<int>, IOnlineOnlyCommand, IOfflineCapableCommand;

    // ══════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════

    private static OfflinePipelineBehavior<TCommand, TResult> CreateBehavior<TCommand, TResult>(
        IOfflineModeService offlineMode)
        where TCommand : ICommandRequest<TResult>
    {
        var logger = NullLogger<OfflinePipelineBehavior<TCommand, TResult>>.Instance;
        return new OfflinePipelineBehavior<TCommand, TResult>(offlineMode, logger);
    }

    private static IOfflineModeService OnlineService()
    {
        var svc = Substitute.For<IOfflineModeService>();
        svc.IsOffline.Returns(false);
        return svc;
    }

    private static IOfflineModeService OfflineService()
    {
        var svc = Substitute.For<IOfflineModeService>();
        svc.IsOffline.Returns(true);
        return svc;
    }

    // ══════════════════════════════════════════════════════════════
    //  ONLINE — all commands pass through
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Online_OnlineOnlyCommand_PassesThrough()
    {
        var behavior = CreateBehavior<OnlineOnlyCmd, int>(OnlineService());

        var result = await behavior.HandleAsync(
            new OnlineOnlyCmd("test"),
            () => Task.FromResult(CommandResult<int>.Success(42)));

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Online_OfflineCapableCommand_PassesThrough()
    {
        var behavior = CreateBehavior<OfflineCapableCmd, string>(OnlineService());

        var result = await behavior.HandleAsync(
            new OfflineCapableCmd("data"),
            () => Task.FromResult(CommandResult<string>.Success("ok")));

        Assert.True(result.Succeeded);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task Online_UnmarkedCommand_PassesThrough()
    {
        var behavior = CreateBehavior<UnmarkedCmd, bool>(OnlineService());

        var result = await behavior.HandleAsync(
            new UnmarkedCmd(99),
            () => Task.FromResult(CommandResult<bool>.Success(true)));

        Assert.True(result.Succeeded);
        Assert.True(result.Value);
    }

    // ══════════════════════════════════════════════════════════════
    //  OFFLINE + IOnlineOnlyCommand → blocked
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Offline_OnlineOnlyCommand_Blocked()
    {
        var behavior = CreateBehavior<OnlineOnlyCmd, int>(OfflineService());
        var handlerCalled = false;

        var result = await behavior.HandleAsync(
            new OnlineOnlyCmd("test"),
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(CommandResult<int>.Success(1));
            });

        Assert.False(result.Succeeded);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Offline_OnlineOnlyCommand_ErrorMessageMentionsConnectivity()
    {
        var behavior = CreateBehavior<OnlineOnlyCmd, int>(OfflineService());

        var result = await behavior.HandleAsync(
            new OnlineOnlyCmd("test"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.Contains("database connection", result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Offline_OnlineOnlyCommand_ErrorMessageMentionsRestored()
    {
        var behavior = CreateBehavior<OnlineOnlyCmd, int>(OfflineService());

        var result = await behavior.HandleAsync(
            new OnlineOnlyCmd("test"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.Contains("restored", result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    // ══════════════════════════════════════════════════════════════
    //  OFFLINE + IOfflineCapableCommand → allowed
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Offline_OfflineCapableCommand_PassesThrough()
    {
        var behavior = CreateBehavior<OfflineCapableCmd, string>(OfflineService());

        var result = await behavior.HandleAsync(
            new OfflineCapableCmd("data"),
            () => Task.FromResult(CommandResult<string>.Success("queued")));

        Assert.True(result.Succeeded);
        Assert.Equal("queued", result.Value);
    }

    [Fact]
    public async Task Offline_OfflineCapableCommand_HandlerCalled()
    {
        var behavior = CreateBehavior<OfflineCapableCmd, string>(OfflineService());
        var handlerCalled = false;

        await behavior.HandleAsync(
            new OfflineCapableCmd("data"),
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(CommandResult<string>.Success("ok"));
            });

        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task Offline_OfflineCapableCommand_HandlerFailure_PropagatedAsIs()
    {
        var behavior = CreateBehavior<OfflineCapableCmd, string>(OfflineService());

        var result = await behavior.HandleAsync(
            new OfflineCapableCmd("bad"),
            () => Task.FromResult(CommandResult<string>.Failure("queue full")));

        Assert.False(result.Succeeded);
        Assert.Equal("queue full", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  OFFLINE + unmarked command → passes through
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Offline_UnmarkedCommand_PassesThrough()
    {
        var behavior = CreateBehavior<UnmarkedCmd, bool>(OfflineService());

        var result = await behavior.HandleAsync(
            new UnmarkedCmd(99),
            () => Task.FromResult(CommandResult<bool>.Success(true)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Offline_UnmarkedCommand_HandlerCalled()
    {
        var behavior = CreateBehavior<UnmarkedCmd, bool>(OfflineService());
        var handlerCalled = false;

        await behavior.HandleAsync(
            new UnmarkedCmd(99),
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(CommandResult<bool>.Success(true));
            });

        Assert.True(handlerCalled);
    }

    // ══════════════════════════════════════════════════════════════
    //  Conflicting markers — IOnlineOnly takes precedence
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Offline_ConflictingMarkers_OnlineOnlyTakesPrecedence()
    {
        var behavior = CreateBehavior<ConflictingCmd, int>(OfflineService());

        var result = await behavior.HandleAsync(
            new ConflictingCmd("test"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Online_ConflictingMarkers_PassesThrough()
    {
        var behavior = CreateBehavior<ConflictingCmd, int>(OnlineService());

        var result = await behavior.HandleAsync(
            new ConflictingCmd("test"),
            () => Task.FromResult(CommandResult<int>.Success(42)));

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    // ══════════════════════════════════════════════════════════════
    //  Marker interface detection
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void OnlineOnlyCmd_ImplementsMarker()
    {
        Assert.IsAssignableFrom<IOnlineOnlyCommand>(new OnlineOnlyCmd("x"));
    }

    [Fact]
    public void OfflineCapableCmd_ImplementsMarker()
    {
        Assert.IsAssignableFrom<IOfflineCapableCommand>(new OfflineCapableCmd("x"));
    }

    [Fact]
    public void UnmarkedCmd_DoesNotImplementEitherMarker()
    {
        var cmd = new UnmarkedCmd(1);
        Assert.IsNotAssignableFrom<IOnlineOnlyCommand>(cmd);
        Assert.IsNotAssignableFrom<IOfflineCapableCommand>(cmd);
    }

    // ══════════════════════════════════════════════════════════════
    //  OfflineModeService interaction
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChecksIsOffline_ExactlyOnce()
    {
        var offlineMode = OfflineService();
        var behavior = CreateBehavior<OnlineOnlyCmd, int>(offlineMode);

        await behavior.HandleAsync(
            new OnlineOnlyCmd("test"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        _ = offlineMode.Received(1).IsOffline;
    }

    // ══════════════════════════════════════════════════════════════
    //  Result fidelity — success values pass through unchanged
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Online_Success_ValuePreserved()
    {
        var behavior = CreateBehavior<OnlineOnlyCmd, int>(OnlineService());

        var result = await behavior.HandleAsync(
            new OnlineOnlyCmd("test"),
            () => Task.FromResult(CommandResult<int>.Success(12345)));

        Assert.Equal(12345, result.Value);
    }

    [Fact]
    public async Task Online_Failure_ErrorPreserved()
    {
        var behavior = CreateBehavior<OnlineOnlyCmd, int>(OnlineService());

        var result = await behavior.HandleAsync(
            new OnlineOnlyCmd("test"),
            () => Task.FromResult(CommandResult<int>.Failure("specific error")));

        Assert.Equal("specific error", result.ErrorMessage);
    }
}
