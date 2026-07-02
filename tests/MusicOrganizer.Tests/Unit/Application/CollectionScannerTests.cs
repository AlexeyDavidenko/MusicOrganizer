using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;

namespace MusicOrganizer.Tests.Unit.Application;

public class CollectionScannerTests
{
    [Fact]
    public async Task ScanAsync_StreamsOneEntryPerFile_InOrder_AndDoesNotStopOnFailure()
    {
        var paths = new[] { "a.mp3", "b.mp3", "c.mp3" };
        var fileSystemScanner = new FakeFileSystemScanner(paths);
        var tagReader = new FakeAudioTagReader(path => path == "b.mp3"
            ? ScanEntry.Failure(path, "corrupt")
            : ScanEntry.Success(path, new AudioTags("Title", "Artist", "Album", 2020, 1, "Rock")));

        var sut = new CollectionScanner(fileSystemScanner, tagReader, NullLogger<CollectionScanner>.Instance);

        var entries = new List<ScanEntry>();
        await foreach (var entry in sut.ScanAsync("root"))
        {
            entries.Add(entry);
        }

        entries.Select(e => e.FilePath).Should().Equal(paths);
        entries[0].Succeeded.Should().BeTrue();
        entries[1].Succeeded.Should().BeFalse();
        entries[1].Error.Should().Be("corrupt");
        entries[2].Succeeded.Should().BeTrue();
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
}
