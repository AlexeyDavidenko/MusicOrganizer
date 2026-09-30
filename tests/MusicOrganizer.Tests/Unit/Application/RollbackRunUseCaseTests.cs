using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Tests.Unit.Application;

public class RollbackRunUseCaseTests
{
    [Fact]
    public async Task RollbackAsync_RestoresEveryEntry()
    {
        var runId = Guid.NewGuid();
        var entries = new[]
        {
            Entry(runId, "a.mp3"),
            Entry(runId, "b.mp3"),
        };
        var journal = new FakeJournal(entries);
        var sut = new RollbackRunUseCase(journal, NullLogger<RollbackRunUseCase>.Instance);

        var restored = await CollectAsync(sut.RollbackAsync(runId));

        restored.Should().HaveCount(2);
        journal.RestoredIds.Should().BeEquivalentTo(entries.Select(e => e.Id));
    }

    [Fact]
    public async Task RollbackAsync_RestoresInReverseChronologicalOrder()
    {
        // A later entry in a run can depend on an earlier one having already happened (e.g.
        // organize's empty-folder pruning removes a folder only after files were moved out of
        // it) - restoring forward would try to move a file back into a folder that hasn't been
        // recreated yet. Undoing last-applied-first is the only order that's always safe.
        var runId = Guid.NewGuid();
        var move = Entry(runId, "flat/nested/a.mp3");
        var pruneNested = Entry(runId, "flat/nested");
        var pruneFlat = Entry(runId, "flat");
        var journal = new FakeJournal([move, pruneNested, pruneFlat]);
        var sut = new RollbackRunUseCase(journal, NullLogger<RollbackRunUseCase>.Instance);

        await CollectAsync(sut.RollbackAsync(runId));

        journal.RestoredIds.Should().Equal(pruneFlat.Id, pruneNested.Id, move.Id);
    }

    private static JournalEntry Entry(Guid runId, string originalPath) =>
        new(Guid.NewGuid(), runId, originalPath, null, null, "organize", DateTimeOffset.UtcNow);

    private static async Task<List<JournalEntry>> CollectAsync(IAsyncEnumerable<JournalEntry> source)
    {
        var results = new List<JournalEntry>();
        await foreach (var item in source)
        {
            results.Add(item);
        }

        return results;
    }

    private sealed class FakeJournal(IReadOnlyList<JournalEntry> entries) : IOperationJournal
    {
        public List<Guid> RestoredIds { get; } = [];

        public Task<JournalEntry> RecordMutationAsync(Guid runId, string filePath, string operationType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<JournalEntry> RecordMoveAsync(Guid runId, string originalPath, string newPath, string operationType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<JournalEntry> RecordDirectoryRemovalAsync(Guid runId, string directoryPath, string operationType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<JournalEntry> GetEntriesAsync(Guid runId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entry;
                await Task.Yield();
            }
        }

        public Task RestoreAsync(JournalEntry entry, CancellationToken cancellationToken = default)
        {
            RestoredIds.Add(entry.Id);
            return Task.CompletedTask;
        }
    }
}
