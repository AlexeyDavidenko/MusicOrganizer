using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Journal;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Tests.Unit.Gui;

public class RollbackViewModelTests
{
    [Fact]
    public async Task RefreshRuns_OrdersNewestFirst()
    {
        var older = new JournalRunSummary(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-5), 2);
        var newer = new JournalRunSummary(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), 1);
        var maintenance = new FakeJournalMaintenance([older, newer]);
        var sut = new RollbackViewModel(NewRollbackUseCase(new FakeJournal()), maintenance);

        await sut.RefreshRunsCommand.ExecuteAsync(null);

        sut.AvailableRuns.Should().Equal(newer, older);
    }

    [Fact]
    public async Task Run_UsesSelectedRunId()
    {
        var runId = Guid.NewGuid();
        var journal = new FakeJournal { EntriesByRun = { [runId] = [Entry(runId, "a.mp3")] } };
        var sut = new RollbackViewModel(NewRollbackUseCase(journal), new FakeJournalMaintenance([]))
        {
            SelectedRun = new JournalRunSummary(runId, DateTimeOffset.UtcNow, 1),
        };

        await sut.RunCommand.ExecuteAsync(null);

        journal.RestoredEntryIds.Should().ContainSingle();
        sut.Results.Should().ContainSingle(e => e.OriginalPath == "a.mp3");
    }

    [Fact]
    public async Task Run_FallsBackToTypedRunId_WhenNoSelection()
    {
        var runId = Guid.NewGuid();
        var journal = new FakeJournal { EntriesByRun = { [runId] = [Entry(runId, "b.mp3")] } };
        var sut = new RollbackViewModel(NewRollbackUseCase(journal), new FakeJournalMaintenance([]))
        {
            RunIdText = runId.ToString(),
        };

        await sut.RunCommand.ExecuteAsync(null);

        sut.Results.Should().ContainSingle(e => e.OriginalPath == "b.mp3");
    }

    [Fact]
    public async Task Run_DoesNothing_WhenNoRunSelectedOrTyped()
    {
        var journal = new FakeJournal();
        var sut = new RollbackViewModel(NewRollbackUseCase(journal), new FakeJournalMaintenance([]));

        await sut.RunCommand.ExecuteAsync(null);

        journal.RestoredEntryIds.Should().BeEmpty();
        sut.StatusMessage.Should().Be("Select a run or enter a valid run id.");
    }

    [Fact]
    public async Task Run_DoesNothing_WhenTypedRunIdIsNotAGuid()
    {
        var journal = new FakeJournal();
        var sut = new RollbackViewModel(NewRollbackUseCase(journal), new FakeJournalMaintenance([]))
        {
            RunIdText = "not-a-guid",
        };

        await sut.RunCommand.ExecuteAsync(null);

        journal.RestoredEntryIds.Should().BeEmpty();
        sut.StatusMessage.Should().Be("Select a run or enter a valid run id.");
    }

    private static RollbackRunUseCase NewRollbackUseCase(IOperationJournal journal) =>
        new(journal, NullLogger<RollbackRunUseCase>.Instance);

    private static JournalEntry Entry(Guid runId, string path) =>
        new(Guid.NewGuid(), runId, path, path + ".bak", null, "rename", DateTimeOffset.UtcNow);

    private sealed class FakeJournalMaintenance(IReadOnlyList<JournalRunSummary> runs) : IJournalMaintenance
    {
        public async IAsyncEnumerable<JournalRunSummary> GetAllRunsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var run in runs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return run;
                await Task.Yield();
            }
        }

        public Task DeleteRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeJournal : IOperationJournal
    {
        public Dictionary<Guid, List<JournalEntry>> EntriesByRun { get; } = [];

        public List<Guid> RestoredEntryIds { get; } = [];

        public async IAsyncEnumerable<JournalEntry> GetEntriesAsync(Guid runId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (EntriesByRun.TryGetValue(runId, out var entries))
            {
                foreach (var entry in entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return entry;
                    await Task.Yield();
                }
            }
        }

        public Task RestoreAsync(JournalEntry entry, CancellationToken cancellationToken = default)
        {
            RestoredEntryIds.Add(entry.Id);
            return Task.CompletedTask;
        }

        public Task<JournalEntry> RecordMutationAsync(Guid runId, string filePath, string operationType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<JournalEntry> RecordMoveAsync(Guid runId, string originalPath, string newPath, string operationType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<JournalEntry> RecordDirectoryRemovalAsync(Guid runId, string directoryPath, string operationType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
