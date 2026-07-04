using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Deduplication;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Tests.Unit.Application;

public class DuplicateRemovalServiceTests
{
    private static readonly DuplicateGroup ExactGroup = new(
        DuplicateMatchKind.Exact,
        "hash",
        ["short.mp3", "much-longer-name.mp3"],
        100);

    private static readonly DuplicateGroup TagMatchGroup = new(
        DuplicateMatchKind.TagMatch,
        "key",
        ["tagmatch-a.mp3", "tagmatch-b.mp3"],
        null);

    [Fact]
    public async Task RemoveAsync_KeepsTheShortestPath_AndRemovesTheRest()
    {
        var finder = new FakeDuplicateFinder([ExactGroup]);
        var remover = new FakeFileRemover(_ => FileRemovalResult.Success("removed"));
        var journal = new FakeJournal();
        var sut = new DuplicateRemovalService(finder, remover, journal, NullLogger<DuplicateRemovalService>.Instance);

        var outcomes = await CollectAsync(sut.RemoveAsync("root", Guid.NewGuid(), dryRun: false));

        outcomes.Should().ContainSingle();
        outcomes[0].FilePath.Should().Be("much-longer-name.mp3");
        outcomes[0].KeptFilePath.Should().Be("short.mp3");
        outcomes[0].Applied.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_DoesNotDeleteOrJournal_WhenDryRun()
    {
        var finder = new FakeDuplicateFinder([ExactGroup]);
        var remover = new FakeFileRemover(_ => FileRemovalResult.Success("removed"));
        var journal = new FakeJournal();
        var sut = new DuplicateRemovalService(finder, remover, journal, NullLogger<DuplicateRemovalService>.Instance);

        var outcomes = await CollectAsync(sut.RemoveAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeFalse();
        remover.CallCount.Should().Be(0);
        journal.RecordCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RemoveAsync_JournalsBeforeDeleting_WhenApplying()
    {
        var finder = new FakeDuplicateFinder([ExactGroup]);
        var callOrder = new List<string>();
        var remover = new FakeFileRemover(path =>
        {
            callOrder.Add("delete");
            return FileRemovalResult.Success(path);
        });
        var journal = new FakeJournal(onRecord: () => callOrder.Add("journal"));
        var sut = new DuplicateRemovalService(finder, remover, journal, NullLogger<DuplicateRemovalService>.Instance);

        await CollectAsync(sut.RemoveAsync("root", Guid.NewGuid(), dryRun: false));

        callOrder.Should().Equal("journal", "delete");
    }

    [Fact]
    public async Task RemoveAsync_NeverTouchesTagMatchGroups()
    {
        var finder = new FakeDuplicateFinder([TagMatchGroup]);
        var remover = new FakeFileRemover(_ => FileRemovalResult.Success("removed"));
        var journal = new FakeJournal();
        var sut = new DuplicateRemovalService(finder, remover, journal, NullLogger<DuplicateRemovalService>.Instance);

        var outcomes = await CollectAsync(sut.RemoveAsync("root", Guid.NewGuid(), dryRun: false));

        outcomes.Should().BeEmpty();
        remover.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task RemoveAsync_ContinuesPastADeletionFailure_OnOtherFiles()
    {
        var groupOfThree = new DuplicateGroup(DuplicateMatchKind.Exact, "hash", ["a.mp3", "bb.mp3", "ccc.mp3"], 100);
        var finder = new FakeDuplicateFinder([groupOfThree]);
        var remover = new FakeFileRemover(path => path == "bb.mp3"
            ? FileRemovalResult.Failure(path, "locked")
            : FileRemovalResult.Success(path));
        var journal = new FakeJournal();
        var sut = new DuplicateRemovalService(finder, remover, journal, NullLogger<DuplicateRemovalService>.Instance);

        var outcomes = await CollectAsync(sut.RemoveAsync("root", Guid.NewGuid(), dryRun: false));

        outcomes.Should().HaveCount(2);
        outcomes.Should().Contain(o => o.FilePath == "bb.mp3" && o.Error == "locked");
        outcomes.Should().Contain(o => o.FilePath == "ccc.mp3" && o.Applied);
    }

    private static async Task<List<DuplicateRemovalOutcome>> CollectAsync(IAsyncEnumerable<DuplicateRemovalOutcome> source)
    {
        var results = new List<DuplicateRemovalOutcome>();
        await foreach (var item in source)
        {
            results.Add(item);
        }

        return results;
    }

    private sealed class FakeDuplicateFinder(IReadOnlyList<DuplicateGroup> groups) : IDuplicateFinder
    {
        public Task<IReadOnlyList<DuplicateGroup>> FindAsync(string rootPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(groups);
    }

    private sealed class FakeFileRemover(Func<string, FileRemovalResult> factory) : IFileRemover
    {
        public int CallCount { get; private set; }

        public Task<FileRemovalResult> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(factory(filePath));
        }
    }

    private sealed class FakeJournal(Action? onRecord = null) : IOperationJournal
    {
        public int RecordCallCount { get; private set; }

        public Task<JournalEntry> RecordMutationAsync(Guid runId, string filePath, string operationType, CancellationToken cancellationToken = default)
        {
            RecordCallCount++;
            onRecord?.Invoke();
            return Task.FromResult(new JournalEntry(Guid.NewGuid(), runId, filePath, filePath + ".bak", null, operationType, DateTimeOffset.UtcNow));
        }

        public Task<JournalEntry> RecordMoveAsync(Guid runId, string originalPath, string newPath, string operationType, CancellationToken cancellationToken = default)
        {
            RecordCallCount++;
            onRecord?.Invoke();
            return Task.FromResult(new JournalEntry(Guid.NewGuid(), runId, originalPath, null, newPath, operationType, DateTimeOffset.UtcNow));
        }

        public async IAsyncEnumerable<JournalEntry> GetEntriesAsync(Guid runId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task RestoreAsync(JournalEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
