using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public sealed class PersistenceFlushSerializationTests
{
    [Fact]
    public void TipStateService_BlockedWrite_PreventsLaterFlushFromBypassingIt()
    {
        var filePath = CreateTempFilePath();
        using var firstWriteEntered = new ManualResetEventSlim(false);
        using var releaseFirstWrite = new ManualResetEventSlim(false);
        var writeCount = 0;

        try
        {
            var sut = new TipStateService(
                filePath,
                NullLogger<TipStateService>.Instance,
                beforeWrite: () =>
                {
                    if (Interlocked.Increment(ref writeCount) != 1)
                        return;

                    firstWriteEntered.Set();
                    if (!releaseFirstWrite.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException("Timed out waiting to release the first tip-state flush.");
                });

            sut.DismissTip("alpha");
            Assert.True(firstWriteEntered.Wait(TimeSpan.FromSeconds(5)));

            sut.DismissTip("beta");

            Thread.Sleep(300);
            Assert.Equal(1, Volatile.Read(ref writeCount));
            Assert.False(File.Exists(filePath));

            releaseFirstWrite.Set();

            Assert.True(SpinWait.SpinUntil(
                () => Volatile.Read(ref writeCount) >= 2
                    && TryReadDismissedTips(filePath, out var keys)
                    && keys.SequenceEqual(["alpha", "beta"]),
                TimeSpan.FromSeconds(10)));
        }
        finally
        {
            DeleteTempFile(filePath);
        }
    }

    [Fact]
    public void OnboardingJourneyService_BlockedWrite_PreventsLaterFlushFromBypassingIt()
    {
        var filePath = CreateTempFilePath();
        using var firstWriteEntered = new ManualResetEventSlim(false);
        using var releaseFirstWrite = new ManualResetEventSlim(false);
        var writeCount = 0;
        var appState = Substitute.For<IAppStateService>();
        var eventBus = Substitute.For<IEventBus>();

        try
        {
            using var sut = new OnboardingJourneyService(
                appState,
                eventBus,
                NullLogger<OnboardingJourneyService>.Instance,
                filePath,
                beforeWrite: () =>
                {
                    if (Interlocked.Increment(ref writeCount) != 1)
                        return;

                    firstWriteEntered.Set();
                    if (!releaseFirstWrite.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException("Timed out waiting to release the first onboarding flush.");
                });

            sut.RecordSessionStart();
            Assert.True(firstWriteEntered.Wait(TimeSpan.FromSeconds(5)));

            sut.RecordWindowOpen("InventoryWindow");

            Thread.Sleep(300);
            Assert.Equal(1, Volatile.Read(ref writeCount));
            Assert.False(File.Exists(filePath));

            releaseFirstWrite.Set();

            Assert.True(SpinWait.SpinUntil(
                () => Volatile.Read(ref writeCount) >= 2
                    && TryReadOnboardingState(filePath, out var sessions, out var totalWindowOpens, out var distinctWindowCount)
                    && sessions == 1
                    && totalWindowOpens == 1
                    && distinctWindowCount == 1,
                TimeSpan.FromSeconds(30)));
        }
        finally
        {
            DeleteTempFile(filePath);
        }
    }

    private static string CreateTempFilePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "StoreAssistantPro.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "state.json");
    }

    private static void DeleteTempFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null && Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private static bool TryReadDismissedTips(string filePath, out string[] keys)
    {
        keys = [];
        if (!File.Exists(filePath))
            return false;

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            keys = JsonSerializer.Deserialize<string[]>(stream) ?? [];
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool TryReadOnboardingState(
        string filePath,
        out int sessions,
        out int totalWindowOpens,
        out int distinctWindowCount)
    {
        sessions = 0;
        totalWindowOpens = 0;
        distinctWindowCount = 0;

        if (!File.Exists(filePath))
            return false;

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            sessions = root.GetProperty("Sessions").GetInt32();
            totalWindowOpens = root.GetProperty("TotalWindowOpens").GetInt32();
            distinctWindowCount = root.GetProperty("DistinctWindows").GetArrayLength();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
