using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Encoding;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.Encoding;
using MusicOrganizer.Domain.Journal;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Tests.Unit.Application;

public class EncodingFixServiceTests
{
    private const string FilePath = "Some Artist - Some Title.mp3";

    static EncodingFixServiceTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public async Task FixAsync_DoesNotWriteOrJournal_WhenDryRun()
    {
        var mojibakeArtist = Mojibake("Максим Фадеев", 1251);
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, TagsFor(mojibakeArtist)));
        var inspector = new FakeEligibilityInspector(_ => new TextFieldEligibility(false, true, false, false));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();
        var sut = new EncodingFixService(scanner, reader, inspector, writer, journal, NullLogger<EncodingFixService>.Instance);

        var outcomes = await CollectAsync(sut.FixAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeFalse();
        outcomes[0].HasFix.Should().BeTrue();
        outcomes[0].FixedFields.Should().Equal("Artist");
        writer.CallCount.Should().Be(0);
        journal.RecordCallCount.Should().Be(0);
    }

    [Fact]
    public async Task FixAsync_RecordsJournalBeforeWriting_WhenApplying()
    {
        var mojibakeArtist = Mojibake("Максим Фадеев", 1251);
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, TagsFor(mojibakeArtist)));
        var inspector = new FakeEligibilityInspector(_ => new TextFieldEligibility(false, true, false, false));
        var callOrder = new List<string>();
        var writer = new FakeTagWriter(_ =>
        {
            callOrder.Add("write");
            return TagWriteResult.Success(FilePath);
        });
        var journal = new FakeJournal(onRecord: () => callOrder.Add("journal"));
        var sut = new EncodingFixService(scanner, reader, inspector, writer, journal, NullLogger<EncodingFixService>.Instance);

        var outcomes = await CollectAsync(sut.FixAsync("root", Guid.NewGuid(), dryRun: false));

        outcomes.Should().ContainSingle();
        outcomes[0].Applied.Should().BeTrue();
        callOrder.Should().Equal("journal", "write");
    }

    [Fact]
    public async Task FixAsync_ReportsNoFixAndNoManualReview_WhenTagsAreAlreadyAscii()
    {
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, TagsFor("Pink Floyd")));
        var inspector = new FakeEligibilityInspector(_ => new TextFieldEligibility(false, true, false, false));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();
        var sut = new EncodingFixService(scanner, reader, inspector, writer, journal, NullLogger<EncodingFixService>.Instance);

        var outcomes = await CollectAsync(sut.FixAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].HasFix.Should().BeFalse();
        outcomes[0].NeedsManualReview.Should().BeFalse();
    }

    [Fact]
    public async Task FixAsync_ReportsManualReview_WhenFieldIsSuspiciousButNotConfidentlyFixed()
    {
        var ambiguous = Mojibake("Ддт", 866);
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Success(FilePath, TagsFor(ambiguous)));
        var inspector = new FakeEligibilityInspector(_ => new TextFieldEligibility(false, true, false, false));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();
        var sut = new EncodingFixService(scanner, reader, inspector, writer, journal, NullLogger<EncodingFixService>.Instance);

        var outcomes = await CollectAsync(sut.FixAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].HasFix.Should().BeFalse();
        outcomes[0].NeedsManualReview.Should().BeTrue();
        journal.RecordCallCount.Should().Be(0);
    }

    [Fact]
    public async Task FixAsync_ContinuesPastAWriteFailure_OnOtherFiles()
    {
        var mojibakeArtist = Mojibake("Максим Фадеев", 1251);
        var paths = new[] { "bad.mp3", "good.mp3" };
        var scanner = new FakeFileSystemScanner(paths);
        var reader = new FakeAudioTagReader(path => ScanEntry.Success(path, TagsFor(mojibakeArtist)));
        var inspector = new FakeEligibilityInspector(_ => new TextFieldEligibility(false, true, false, false));
        var writer = new FakeTagWriter(path => path == "bad.mp3"
            ? TagWriteResult.Failure(path, "disk full")
            : TagWriteResult.Success(path));
        var journal = new FakeJournal();
        var sut = new EncodingFixService(scanner, reader, inspector, writer, journal, NullLogger<EncodingFixService>.Instance);

        var outcomes = await CollectAsync(sut.FixAsync("root", Guid.NewGuid(), dryRun: false));

        outcomes.Should().HaveCount(2);
        outcomes[0].Error.Should().Be("disk full");
        outcomes[1].Applied.Should().BeTrue();
    }

    [Fact]
    public async Task FixAsync_DoesNotFlagManualReview_WhenFileErrored()
    {
        var scanner = new FakeFileSystemScanner([FilePath]);
        var reader = new FakeAudioTagReader(_ => ScanEntry.Failure(FilePath, "corrupt"));
        var inspector = new FakeEligibilityInspector(_ => new TextFieldEligibility(true, true, true, true));
        var writer = new FakeTagWriter(_ => TagWriteResult.Success(FilePath));
        var journal = new FakeJournal();
        var sut = new EncodingFixService(scanner, reader, inspector, writer, journal, NullLogger<EncodingFixService>.Instance);

        var outcomes = await CollectAsync(sut.FixAsync("root", Guid.NewGuid(), dryRun: true));

        outcomes.Should().ContainSingle();
        outcomes[0].Error.Should().Be("corrupt");
        outcomes[0].NeedsManualReview.Should().BeFalse();
    }

    private static string Mojibake(string real, int codePage) =>
        Encoding.Latin1.GetString(Encoding.GetEncoding(codePage).GetBytes(real));

    private static AudioTags TagsFor(string artist) =>
        new(Title: null, Artist: artist, Album: null, Year: null, TrackNumber: null, Genre: null);

    private static async Task<List<EncodingFixOutcome>> CollectAsync(IAsyncEnumerable<EncodingFixOutcome> source)
    {
        var results = new List<EncodingFixOutcome>();
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

    private sealed class FakeEligibilityInspector(Func<string, TextFieldEligibility> factory) : IEncodingEligibilityInspector
    {
        public Task<TextFieldEligibility> InspectAsync(string filePath, CancellationToken cancellationToken = default) =>
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

        public Task<JournalEntry> RecordDirectoryRemovalAsync(Guid runId, string directoryPath, string operationType, CancellationToken cancellationToken = default)
        {
            RecordCallCount++;
            onRecord?.Invoke();
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
