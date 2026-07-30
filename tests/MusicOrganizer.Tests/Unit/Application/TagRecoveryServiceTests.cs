using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.Journal;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Tests.Unit.Application;

public class TagRecoveryServiceTests
{
    private const string FilePath = "Some Artist - Some Title.mp3";

    private static readonly ITagRecoverySource[] DefaultSources =
    [
        new FakeTagRecoverySource(current =>
        {
            var recovered = new List<string>();
            var artist = current.Artist;
            var title = current.Title;

            if (artist is null)
            {
                artist = "Some Artist";
                recovered.Add("Artist");
            }

            if (title is null)
            {
                title = "Some Title";
                recovered.Add("Title");
            }

            return new TagRecoveryProposal(current with { Artist = artist, Title = title }, recovered);
        }),
    ];

    [Fact]
    public async Task RecoverAsync_DoesNotWriteOrJournal_WhenDryRun()
    {
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, EmptyTags()));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();
        var sut = new TagRecoveryService(scanner, reader, writer, journal, DefaultSources, NullLogger<TagRecoveryService>.Instance);

        var outcomes = await CollectAsync(sut.RecoverAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeFalse();
        outcomes[0].HasRecovery.Should().BeTrue();
        writer.CallCount.Should().Be(0);
        journal.RecordCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RecoverAsync_RecordsJournalBeforeWriting_WhenApplying()
    {
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, EmptyTags()));
        var callOrder = new List<string>();
        var writer = new FakeTagWriter(path =>
        {
            callOrder.Add("write");
            return TagWriteResult.Success(path);
        });
        var journal = new FakeJournal(onRecord: () => callOrder.Add("journal"));
        var sut = new TagRecoveryService(scanner, reader, writer, journal, DefaultSources, NullLogger<TagRecoveryService>.Instance);

        var outcomes = await CollectAsync(sut.RecoverAsync("root", Guid.NewGuid(), dryRun: false));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeTrue();
        callOrder.Should().Equal("journal", "write");
    }

    [Fact]
    public async Task RecoverAsync_ContinuesPastAWriteFailure_OnOtherFiles()
    {
        var paths = new[] { "bad - file.mp3", "good - file.mp3" };
        var scanner = new FakeFileSystemScanner(paths);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, EmptyTags()));
        var writer = new FakeTagWriter(path => path == "bad - file.mp3"
            ? TagWriteResult.Failure(path, "disk full")
            : TagWriteResult.Success(path));
        var journal = new FakeJournal();
        var sut = new TagRecoveryService(scanner, reader, writer, journal, DefaultSources, NullLogger<TagRecoveryService>.Instance);

        var outcomes = await CollectAsync(sut.RecoverAsync("root", Guid.NewGuid(), dryRun: false));

        outcomes.Should().HaveCount(2);
        outcomes[0].Error.Should().Be("disk full");
        outcomes[1].Applied.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverAsync_RunsSourcesInOrder_SoALaterSourceOnlyFillsWhatAnEarlierOneLeftMissing()
    {
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, EmptyTags()));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();

        var firstSource = new FakeTagRecoverySource(current =>
            new TagRecoveryProposal(current with { Artist = "First Artist" }, ["Artist"]));
        var secondSource = new FakeTagRecoverySource(current =>
        {
            if (current.Artist is not null)
            {
                // Would overwrite if not for chain ordering - must not happen.
                return new TagRecoveryProposal(current with { Artist = "Second Artist" }, ["Artist"]);
            }

            return new TagRecoveryProposal(current with { Title = "Second Title" }, ["Title"]);
        });

        var sut = new TagRecoveryService(
            scanner, reader, writer, journal,
            [firstSource, secondSource],
            NullLogger<TagRecoveryService>.Instance);

        var outcomes = await CollectAsync(sut.RecoverAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].RecoveredFields.Should().Contain("Artist");
    }

    [Fact]
    public async Task RecoverAsync_FlagsNeedsManualReview_WhenNoSourceCanFillArtistOrTitle()
    {
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, EmptyTags()));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();
        var noopSource = new FakeTagRecoverySource(current => new TagRecoveryProposal(current, []));
        var sut = new TagRecoveryService(scanner, reader, writer, journal, [noopSource], NullLogger<TagRecoveryService>.Instance);

        var outcomes = await CollectAsync(sut.RecoverAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].NeedsManualReview.Should().BeTrue();
    }

    [Fact]
    public async Task RecoverAsync_DoesNotFlagNeedsManualReview_WhenTagsAreAlreadyComplete()
    {
        var scanner = new FakeFileSystemScanner([FilePath]);
        var fullTags = EmptyTags() with { Artist = "Artist", Title = "Title" };
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, fullTags));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();
        var noopSource = new FakeTagRecoverySource(current => new TagRecoveryProposal(current, []));
        var sut = new TagRecoveryService(scanner, reader, writer, journal, [noopSource], NullLogger<TagRecoveryService>.Instance);

        var outcomes = await CollectAsync(sut.RecoverAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].NeedsManualReview.Should().BeFalse();
    }

    [Fact]
    public async Task RecoverAsync_DoesNotFlagNeedsManualReview_WhenFileErrored()
    {
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Failure(FilePath, "corrupt"));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();
        var sut = new TagRecoveryService(scanner, reader, writer, journal, DefaultSources, NullLogger<TagRecoveryService>.Instance);

        var outcomes = await CollectAsync(sut.RecoverAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].Error.Should().Be("corrupt");
        outcomes[0].NeedsManualReview.Should().BeFalse();
    }

    [Fact]
    public async Task RecoverAsync_BuildsFolderStatisticsContext_FromAllFilesInTheRun()
    {
        var folder = Path.Combine("root", "folder");
        var pathA = Path.Combine(folder, "a.mp3");
        var pathB = Path.Combine(folder, "b.mp3");
        var pathC = Path.Combine(folder, "c.mp3");
        var tagsByPath = new Dictionary<string, AudioTags>
        {
            [pathA] = EmptyTags() with { Artist = "Consensus Artist" },
            [pathB] = EmptyTags() with { Artist = "Consensus Artist" },
            [pathC] = EmptyTags(),
        };
        var scanner = new FakeFileSystemScanner([pathA, pathB, pathC]);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, tagsByPath[path]));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(string.Empty));
        var journal = new FakeJournal();
        var recordingSource = new FakeTagRecoverySource(current => new TagRecoveryProposal(current, []));
        var sut = new TagRecoveryService(scanner, reader, writer, journal, [recordingSource], NullLogger<TagRecoveryService>.Instance);

        await CollectAsync(sut.RecoverAsync("root", Guid.NewGuid(), dryRun: true));

        recordingSource.LastContext.Should().NotBeNull();
        recordingSource.LastContext!.FolderStatistics[folder].Artist.Should().Be("Consensus Artist");
    }

    private static AudioTags EmptyTags() => new(Title: null, Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);

    private static async Task<List<TagRecoveryOutcome>> CollectAsync(IAsyncEnumerable<TagRecoveryOutcome> source)
    {
        var results = new List<TagRecoveryOutcome>();
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

    private sealed class FakeTagWriter(Func<string, TagWriteResult> factory) : ITagWriter
    {
        public int CallCount { get; private set; }

        public Task<TagWriteResult> WriteTagsAsync(string filePath, AudioTags tags, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(factory(filePath));
        }
    }

    private sealed class FakeTagRecoverySource(Func<AudioTags, TagRecoveryProposal> propose) : ITagRecoverySource
    {
        public TagRecoveryContext? LastContext { get; private set; }

        public TagRecoveryProposal Propose(string filePath, string rootPath, AudioTags current, TagRecoveryContext context)
        {
            LastContext = context;
            return propose(current);
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
