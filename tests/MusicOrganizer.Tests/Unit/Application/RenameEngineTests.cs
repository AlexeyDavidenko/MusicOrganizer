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

public class RenameEngineTests
{
    private static readonly string RootPath = Path.Combine("music");
    private static readonly string OriginalPath = Path.Combine(RootPath, "original.mp3");
    private static readonly string ProposedPath = Path.Combine(RootPath, "Artist-Title.mp3");

    [Fact]
    public async Task RenameAsync_DoesNotRenameOrJournal_WhenDryRun()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Artist", "Title")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var sut = new RenameEngine(scanner, reader, renamer, journal, NullLogger<RenameEngine>.Instance);

        var outcomes = await CollectAsync(sut.RenameAsync(RootPath, Guid.NewGuid(), dryRun: true, transliterate: false));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeFalse();
        outcomes[0].NeedsRename.Should().BeTrue();
        renamer.RenameCallCount.Should().Be(0);
        journal.RecordCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RenameAsync_JournalsBeforeRenaming_WhenApplying()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Artist", "Title")));
        var callOrder = new List<string>();
        var renamer = new FakeFileRenamer(
            exists: _ => false,
            onRename: () => callOrder.Add("rename"));
        var journal = new FakeJournal(onRecordMove: () => callOrder.Add("journal"));
        var sut = new RenameEngine(scanner, reader, renamer, journal, NullLogger<RenameEngine>.Instance);

        var outcomes = await CollectAsync(sut.RenameAsync(RootPath, Guid.NewGuid(), dryRun: false, transliterate: false));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeTrue();
        callOrder.Should().Equal("journal", "rename");
    }

    [Fact]
    public async Task RenameAsync_SkipsWithoutError_WhenArtistOrTitleIsMissing()
    {
        var scanner = new FakeFileSystemScanner([Path.Combine(RootPath, "unknown.mp3")]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor(null, "Title")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var sut = new RenameEngine(scanner, reader, renamer, journal, NullLogger<RenameEngine>.Instance);

        var outcomes = await CollectAsync(sut.RenameAsync(RootPath, Guid.NewGuid(), dryRun: false, transliterate: false));

        outcomes.Should().ContainSingle();
        outcomes[0].Error.Should().BeNull();
        outcomes[0].NeedsRename.Should().BeFalse();
        renamer.RenameCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RenameAsync_ResolvesNameCollisions_ToTheNextAvailableCandidate()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Artist", "Title")));
        var renamer = new FakeFileRenamer(exists: path => path == ProposedPath);
        var journal = new FakeJournal();
        var sut = new RenameEngine(scanner, reader, renamer, journal, NullLogger<RenameEngine>.Instance);

        var outcomes = await CollectAsync(sut.RenameAsync(RootPath, Guid.NewGuid(), dryRun: true, transliterate: false));

        outcomes.Should().ContainSingle();
        outcomes[0].ProposedPath.Should().Be(Path.Combine(RootPath, "Artist-Title (1).mp3"));
    }

    [Fact]
    public async Task RenameAsync_ContinuesPastARenameFailure_OnOtherFiles()
    {
        var pathA = Path.Combine(RootPath, "a.mp3");
        var pathB = Path.Combine(RootPath, "b.mp3");
        var scanner = new FakeFileSystemScanner([pathA, pathB]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Artist", "Title")));
        var renamer = new FakeFileRenamer(
            exists: _ => false,
            renameResult: path => path == pathA
                ? RenameResult.Failure(path, "disk full")
                : RenameResult.Success(path, ProposedPath));
        var journal = new FakeJournal();
        var sut = new RenameEngine(scanner, reader, renamer, journal, NullLogger<RenameEngine>.Instance);

        var outcomes = await CollectAsync(sut.RenameAsync(RootPath, Guid.NewGuid(), dryRun: false, transliterate: false));

        outcomes.Should().HaveCount(2);
        outcomes[0].Error.Should().Be("disk full");
        outcomes[1].Applied.Should().BeTrue();
    }

    [Fact]
    public async Task RenameAsync_TransliteratesCyrillicArtistAndTitle_WhenTransliterateIsTrue()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Максим Фадеев", "Лети За Мной")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var sut = new RenameEngine(scanner, reader, renamer, journal, NullLogger<RenameEngine>.Instance);

        var outcomes = await CollectAsync(sut.RenameAsync(RootPath, Guid.NewGuid(), dryRun: true, transliterate: true));

        outcomes.Should().ContainSingle();
        outcomes[0].ProposedPath.Should().Be(Path.Combine(RootPath, "Maksim_Fadeyev-Leti_Za_Mnoy.mp3"));
    }

    [Fact]
    public async Task RenameAsync_KeepsOriginalAlphabet_WhenTransliterateIsFalse()
    {
        var scanner = new FakeFileSystemScanner([OriginalPath]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor("Максим Фадеев", "Лети За Мной")));
        var renamer = new FakeFileRenamer(exists: _ => false);
        var journal = new FakeJournal();
        var sut = new RenameEngine(scanner, reader, renamer, journal, NullLogger<RenameEngine>.Instance);

        var outcomes = await CollectAsync(sut.RenameAsync(RootPath, Guid.NewGuid(), dryRun: true, transliterate: false));

        outcomes.Should().ContainSingle();
        outcomes[0].ProposedPath.Should().Be(Path.Combine(RootPath, "Максим_Фадеев-Лети_За_Мной.mp3"));
    }

    private static AudioTags TagsFor(string? artist, string? title) =>
        new(Title: title, Artist: artist, Album: null, Year: null, TrackNumber: null, Genre: null);

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

    private sealed class FakeJournal(Action? onRecordMove = null) : IOperationJournal
    {
        public int RecordCallCount { get; private set; }

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

        public async IAsyncEnumerable<JournalEntry> GetEntriesAsync(Guid runId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task RestoreAsync(JournalEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
