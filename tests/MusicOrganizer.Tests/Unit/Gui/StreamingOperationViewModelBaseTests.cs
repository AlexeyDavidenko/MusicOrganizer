using System.Runtime.CompilerServices;
using FluentAssertions;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Tests.Unit.Gui;

public class StreamingOperationViewModelBaseTests
{
    [Fact]
    public async Task Run_StreamsEveryOutcomeIntoResults()
    {
        var sut = new TestStreamingViewModel((_, _, _) => Stream("a", "b", "c"));

        await sut.RunCommand.ExecuteAsync(null);

        sut.Results.Should().Equal("a", "b", "c");
    }

    [Fact]
    public async Task Run_SetsLastRunId_AndPassesItToExecute()
    {
        var sut = new TestStreamingViewModel((_, _, _) => Stream("a"));

        await sut.RunCommand.ExecuteAsync(null);

        sut.LastRunId.Should().NotBeNull();
        sut.ReceivedRunId.Should().Be(sut.LastRunId);
    }

    [Fact]
    public async Task Run_IsRunningIsTrueDuringAndFalseAfter()
    {
        TestStreamingViewModel sut = null!;
        bool? runningDuring = null;
        sut = new TestStreamingViewModel((_, _, _) => Observe(() => runningDuring = sut.IsRunning, "a"));

        await sut.RunCommand.ExecuteAsync(null);

        runningDuring.Should().BeTrue();
        sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Run_PassesDryRunTrue_WhenApplyIsFalse()
    {
        var sut = new TestStreamingViewModel((_, _, _) => Stream("a")) { Apply = false };

        await sut.RunCommand.ExecuteAsync(null);

        sut.ReceivedDryRun.Should().BeTrue();
    }

    [Fact]
    public async Task Run_PassesDryRunFalse_WhenApplyIsTrue()
    {
        var sut = new TestStreamingViewModel((_, _, _) => Stream("a")) { Apply = true };

        await sut.RunCommand.ExecuteAsync(null);

        sut.ReceivedDryRun.Should().BeFalse();
    }

    [Fact]
    public async Task Run_ClearsResultsFromThePreviousRun()
    {
        var sut = new TestStreamingViewModel((_, _, _) => Stream("a", "b"));
        await sut.RunCommand.ExecuteAsync(null);

        sut.NextStream = (_, _, _) => Stream("c");
        await sut.RunCommand.ExecuteAsync(null);

        sut.Results.Should().Equal("c");
    }

    [Fact]
    public async Task Run_ReportsProcessedCount_OnSuccess()
    {
        var sut = new TestStreamingViewModel((_, _, _) => Stream("a", "b"));

        await sut.RunCommand.ExecuteAsync(null);

        sut.StatusMessage.Should().Be("Processed 2 item(s).");
    }

    [Fact]
    public async Task Run_ReportsFailure_WhenExecuteThrows()
    {
        var sut = new TestStreamingViewModel((_, _, _) => Throwing());

        await sut.RunCommand.ExecuteAsync(null);

        sut.IsRunning.Should().BeFalse();
        sut.StatusMessage.Should().StartWith("Failed:").And.Contain("boom");
    }

    [Fact]
    public async Task Run_ReportsCancellation_WhenCancelled()
    {
        var started = new TaskCompletionSource();
        var sut = new TestStreamingViewModel((_, _, ct) => BlockUntilCancelled(started, ct));

        var task = sut.RunCommand.ExecuteAsync(null);
        await started.Task;
        sut.RunCancelCommand.Execute(null);
        await task;

        sut.IsRunning.Should().BeFalse();
        sut.StatusMessage.Should().StartWith("Cancelled");
    }

    private static async IAsyncEnumerable<string> Stream(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<string> Observe(Action onStart, params string[] items)
    {
        onStart();
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<string> Throwing()
    {
        yield return "partial";
        await Task.Yield();
        throw new InvalidOperationException("boom");
    }

    private static async IAsyncEnumerable<string> BlockUntilCancelled(TaskCompletionSource started, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "first";
        started.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        yield return "unreachable";
    }

    private sealed class TestStreamingViewModel : StreamingOperationViewModelBase<string>
    {
        public TestStreamingViewModel(Func<Guid, bool, CancellationToken, IAsyncEnumerable<string>> stream)
        {
            NextStream = stream;
        }

        public Func<Guid, bool, CancellationToken, IAsyncEnumerable<string>> NextStream { get; set; }

        public Guid? ReceivedRunId { get; private set; }

        public bool? ReceivedDryRun { get; private set; }

        protected override IAsyncEnumerable<string> ExecuteAsync(Guid runId, bool dryRun, CancellationToken cancellationToken)
        {
            ReceivedRunId = runId;
            ReceivedDryRun = dryRun;
            return NextStream(runId, dryRun, cancellationToken);
        }
    }
}
