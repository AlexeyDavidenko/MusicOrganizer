using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Tests.Unit.Application;

public class CleanJournalUseCaseTests
{
    [Fact]
    public async Task CleanAsync_ReportsOnlyRunsOlderThanTheCutoff()
    {
        var oldRun = new JournalRunSummary(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-40), 3);
        var recentRun = new JournalRunSummary(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), 1);
        var maintenance = new FakeJournalMaintenance([oldRun, recentRun]);
        var sut = new CleanJournalUseCase(maintenance, NullLogger<CleanJournalUseCase>.Instance);

        var outcomes = await CollectAsync(sut.CleanAsync(TimeSpan.FromDays(30), dryRun: true));

        outcomes.Should().ContainSingle(o => o.RunId == oldRun.RunId);
    }

    [Fact]
    public async Task CleanAsync_DoesNotDelete_WhenDryRun()
    {
        var oldRun = new JournalRunSummary(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-40), 3);
        var maintenance = new FakeJournalMaintenance([oldRun]);
        var sut = new CleanJournalUseCase(maintenance, NullLogger<CleanJournalUseCase>.Instance);

        var outcomes = await CollectAsync(sut.CleanAsync(TimeSpan.FromDays(30), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeFalse();
        maintenance.DeletedRunIds.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanAsync_Deletes_WhenApplying()
    {
        var oldRun = new JournalRunSummary(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-40), 3);
        var maintenance = new FakeJournalMaintenance([oldRun]);
        var sut = new CleanJournalUseCase(maintenance, NullLogger<CleanJournalUseCase>.Instance);

        var outcomes = await CollectAsync(sut.CleanAsync(TimeSpan.FromDays(30), dryRun: false));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeTrue();
        maintenance.DeletedRunIds.Should().Equal(oldRun.RunId);
    }

    private static async Task<List<JournalCleanupOutcome>> CollectAsync(IAsyncEnumerable<JournalCleanupOutcome> source)
    {
        var results = new List<JournalCleanupOutcome>();
        await foreach (var item in source)
        {
            results.Add(item);
        }

        return results;
    }

    private sealed class FakeJournalMaintenance(IReadOnlyList<JournalRunSummary> runs) : IJournalMaintenance
    {
        public List<Guid> DeletedRunIds { get; } = [];

        public async IAsyncEnumerable<JournalRunSummary> GetAllRunsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var run in runs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return run;
                await Task.Yield();
            }
        }

        public Task DeleteRunAsync(Guid runId, CancellationToken cancellationToken = default)
        {
            DeletedRunIds.Add(runId);
            return Task.CompletedTask;
        }
    }
}
