using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Renaming;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.Journal;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Tests.Unit.Application;

public class OrganizeEngineTests
{
    private static readonly string RootPath = Path.Combine("music");
    private static readonly string OriginalPath = Path.Combine(RootPath, "flat", "track.mp3");
    private static readonly string ProposedPath = Path.Combine(RootPath, "Pink_Floyd", "The_Wall", "track.mp3");

    [Fact]
    public async Task OrganizeAsync_DoesNotMoveOrJournal_WhenDryRun()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, new FakeDirectoryPruner(), NullLogger<OrganizeEngine>.Instance);

        var outcomes = await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: true, pruneEmptyFolders: false));

        outcomes.Should().ContainSingle();
        outcomes[0].ProposedPath.Should().Be(ProposedPath);
        outcomes[0].Applied.Should().BeFalse();
        outcomes[0].NeedsRename.Should().BeTrue();
        renamer.RenameCallCount.Should().Be(0);
        journal.RecordCallCount.Should().Be(0);
    }

    [Fact]
    public async Task OrganizeAsync_JournalsBeforeMoving_WhenApplying()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var callOrder = new List<string>();
        var renamer = new FakeFileRenamer(
            exists: _ => false,
            onRename: () => callOrder.Add("move"));
        var journal = new FakeJournal(onRecordMove: () => callOrder.Add("journal"));
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, new FakeDirectoryPruner(), NullLogger<OrganizeEngine>.Instance);

        var outcomes = await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: false, pruneEmptyFolders: false));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeTrue();
        callOrder.Should().Equal("journal", "move");
    }

    [Fact]
    public async Task OrganizeAsync_SkipsWithoutError_WhenArtistIsMissing()
    {
        var scanner = new FakeFileSystemScanner([Path.Combine(RootPath, "unknown.mp3")]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor(null, "The Wall")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, new FakeDirectoryPruner(), NullLogger<OrganizeEngine>.Instance);

        var outcomes = await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: false, pruneEmptyFolders: false));

        outcomes.Should().ContainSingle();
        outcomes[0].Error.Should().BeNull();
        outcomes[0].NeedsRename.Should().BeFalse();
        renamer.RenameCallCount.Should().Be(0);
    }

    [Fact]
    public async Task OrganizeAsync_FallsBackToArtistOnlyFolder_WhenAlbumIsMissing()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", null)));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, new FakeDirectoryPruner(), NullLogger<OrganizeEngine>.Instance);

        var outcomes = await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: true, pruneEmptyFolders: false));

        outcomes.Should().ContainSingle();
        outcomes[0].ProposedPath.Should().Be(Path.Combine(RootPath, "Pink_Floyd", "track.mp3"));
    }

    [Fact]
    public async Task OrganizeAsync_ResolvesNameCollisions_ToTheNextAvailableCandidate()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var renamer = new FakeFileRenamer(exists: path => path == ProposedPath);
        var journal = new FakeJournal();
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, new FakeDirectoryPruner(), NullLogger<OrganizeEngine>.Instance);

        var outcomes = await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: true, pruneEmptyFolders: false));

        outcomes.Should().ContainSingle();
        outcomes[0].ProposedPath.Should().Be(Path.Combine(RootPath, "Pink_Floyd", "The_Wall", "track (1).mp3"));
    }

    [Fact]
    public async Task OrganizeAsync_ContinuesPastAMoveFailure_OnOtherFiles()
    {
        var pathA = Path.Combine(RootPath, "flat", "a.mp3");
        var pathB = Path.Combine(RootPath, "flat", "b.mp3");
        var scanner = new FakeFileSystemScanner([pathA, pathB]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var renamer = new FakeFileRenamer(
            exists: _ => false,
            renameResult: path => path == pathA
                ? RenameResult.Failure(path, "disk full")
                : RenameResult.Success(path, ProposedPath));
        var journal = new FakeJournal();
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, new FakeDirectoryPruner(), NullLogger<OrganizeEngine>.Instance);

        var outcomes = await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: false, pruneEmptyFolders: false));

        outcomes.Should().HaveCount(2);
        outcomes[0].Error.Should().Be("disk full");
        outcomes[1].Applied.Should().BeTrue();
    }

    [Fact]
    public async Task OrganizeAsync_PrunesSourceFolder_WhenApplyingWithPruneFlag()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var pruner = new FakeDirectoryPruner();
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, pruner, NullLogger<OrganizeEngine>.Instance);

        await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: false, pruneEmptyFolders: true));

        var expectedSourceDirectory = Path.Combine(RootPath, "flat");
        pruner.RemovedDirectories.Should().Contain(expectedSourceDirectory);
        journal.RemovedDirectories.Should().Contain(expectedSourceDirectory);
    }

    [Fact]
    public async Task OrganizeAsync_DoesNotPrune_WhenDryRun()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var pruner = new FakeDirectoryPruner();
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, pruner, NullLogger<OrganizeEngine>.Instance);

        await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: true, pruneEmptyFolders: true));

        pruner.RemovedDirectories.Should().BeEmpty();
    }

    [Fact]
    public async Task OrganizeAsync_DoesNotPrune_WhenFlagIsFalse()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var pruner = new FakeDirectoryPruner();
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, pruner, NullLogger<OrganizeEngine>.Instance);

        await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: false, pruneEmptyFolders: false));

        pruner.RemovedDirectories.Should().BeEmpty();
    }

    [Fact]
    public async Task OrganizeAsync_ClimbsUpwardPruning_UntilAFolderIsNotEmpty_ButNeverPrunesTheRoot()
    {
        var nestedPath = Path.Combine(RootPath, "flat", "nested", "track.mp3");
        var scanner = new FakeFileSystemScanner([nestedPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var flatDirectory = Path.Combine(RootPath, "flat");
        // "nested" and its parent "flat" are both empty after the move; "flat"'s own parent is
        // RootPath itself, which must never be offered to the pruner at all.
        var pruner = new FakeDirectoryPruner(canRemove: dir => !string.Equals(dir, RootPath, StringComparison.OrdinalIgnoreCase));
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, pruner, NullLogger<OrganizeEngine>.Instance);

        await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: false, pruneEmptyFolders: true));

        pruner.RemovedDirectories.Should().Equal(Path.Combine(flatDirectory, "nested"), flatDirectory);
        pruner.RemovedDirectories.Should().NotContain(RootPath);
    }

    [Fact]
    public async Task OrganizeAsync_StopsClimbing_WhenAFolderIsNotEmpty()
    {
        var nestedPath = Path.Combine(RootPath, "flat", "nested", "track.mp3");
        var scanner = new FakeFileSystemScanner([nestedPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Pink Floyd", "The Wall")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var flatDirectory = Path.Combine(RootPath, "flat");
        // "nested" is empty and gets removed, but "flat" still has other content -> climb stops.
        var pruner = new FakeDirectoryPruner(canRemove: dir => !string.Equals(dir, flatDirectory, StringComparison.OrdinalIgnoreCase));
        var sut = new OrganizeEngine(scanner, reader, renamer, journal, pruner, NullLogger<OrganizeEngine>.Instance);

        await CollectAsync(sut.OrganizeAsync(RootPath, Guid.NewGuid(), dryRun: false, pruneEmptyFolders: true));

        pruner.RemovedDirectories.Should().Equal(Path.Combine(flatDirectory, "nested"));
    }

    private static AudioTags TagsFor(string? artist, string? album) =>
        new(Title: "Title", Artist: artist, Album: album, Year: null, TrackNumber: null, Genre: null);

    private static async Task<List<RenameOutcome>> CollectAsync(IAsyncEnumerable<RenameOutcome> source)
    {
        var results = new List<RenameOutcome>();
        await foreach (var item in source)
        {
            results.Add(item);
        }

        return results;
    }

    private sealed class FakeFileSystemScanner(IReadOnlyList<string> paths) : IFileSystemScanner
    {
        public async IAsyncEnumerable<string> EnumerateAudioFilesAsync(
            string rootPath,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return path;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeAudioTagReader(Func<string, ScanEntry> factory) : IAudioTagReader
    {
        public Task<ScanEntry> ReadTagsAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(factory(filePath));
    }

    private sealed class FakeFileRenamer(
        Func<string, bool> exists,
        Func<string, RenameResult>? renameResult = null,
        Action? onRename = null) : IFileRenamer
    {
        public int RenameCallCount { get; private set; }

        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(exists(path));

        public Task<RenameResult> RenameAsync(string originalPath, string newPath, CancellationToken cancellationToken = default)
        {
            RenameCallCount++;
            onRename?.Invoke();
            var result = renameResult?.Invoke(originalPath) ?? RenameResult.Success(originalPath, newPath);
            return Task.FromResult(result);
        }
    }

    private sealed class FakeDirectoryPruner(Func<string, bool>? canRemove = null) : IDirectoryPruner
    {
        public List<string> RemovedDirectories { get; } = [];

        public Task<bool> TryRemoveIfEmptyAsync(string directory, CancellationToken cancellationToken = default)
        {
            var removed = canRemove?.Invoke(directory) ?? true;
            if (removed)
            {
                RemovedDirectories.Add(directory);
            }

            return Task.FromResult(removed);
        }
    }

    private sealed class FakeJournal(Action? onRecordMove = null) : IOperationJournal
    {
        public int RecordCallCount { get; private set; }

        public List<string> RemovedDirectories { get; } = [];

        public Task<JournalEntry> RecordMutationAsync(Guid runId, string filePath, string operationType, CancellationToken cancellationToken = default)
        {
            RecordCallCount++;
            return Task.FromResult(new JournalEntry(Guid.NewGuid(), runId, filePath, filePath + ".bak", null, operationType, DateTimeOffset.UtcNow));
        }

        public Task<JournalEntry> RecordMoveAsync(Guid runId, string originalPath, string newPath, string operationType, CancellationToken cancellationToken = default)
        {
            RecordCallCount++;
            onRecordMove?.Invoke();
            return Task.FromResult(new JournalEntry(Guid.NewGuid(), runId, originalPath, null, newPath, operationType, DateTimeOffset.UtcNow));
        }

        public Task<JournalEntry> RecordDirectoryRemovalAsync(Guid runId, string directoryPath, string operationType, CancellationToken cancellationToken = default)
        {
            RecordCallCount++;
            RemovedDirectories.Add(directoryPath);
            return Task.FromResult(new JournalEntry(Guid.NewGuid(), runId, directoryPath, null, null, operationType, DateTimeOffset.UtcNow));
        }

        public async IAsyncEnumerable<JournalEntry> GetEntriesAsync(Guid runId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task RestoreAsync(JournalEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
