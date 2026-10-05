using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Tests.Unit.Gui;

public class ScanViewModelTests
{
    [Fact]
    public async Task Run_StreamsEveryScannedEntryIntoResults()
    {
        var scanner = NewScanner(
            ["a.mp3", "b.mp3"],
            path => ScanEntry.Success(path, new AudioTags("Title", "Artist", null, null, null, null)));
        var sut = new ScanViewModel(scanner) { RootPath = "root" };

        await sut.RunCommand.ExecuteAsync(null);

        sut.Results.Select(e => e.FilePath).Should().Equal("a.mp3", "b.mp3");
    }

    [Fact]
    public async Task Run_ReportsScannedCountInStatus()
    {
        var scanner = NewScanner(
            ["a.mp3", "b.mp3", "c.mp3"],
            path => ScanEntry.Success(path, new AudioTags(null, null, null, null, null, null)));
        var sut = new ScanViewModel(scanner) { RootPath = "root" };

        await sut.RunCommand.ExecuteAsync(null);

        sut.StatusMessage.Should().Be("Scanned 3 file(s).");
        sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Run_IncludesFailedEntries()
    {
        var scanner = NewScanner(["bad.mp3"], _ => ScanEntry.Failure("bad.mp3", "corrupt"));
        var sut = new ScanViewModel(scanner) { RootPath = "root" };

        await sut.RunCommand.ExecuteAsync(null);

        sut.Results.Should().ContainSingle(e => !e.Succeeded && e.Error == "corrupt");
    }

    private static CollectionScanner NewScanner(IReadOnlyList<string> paths, Func<string, ScanEntry> read) =>
        new(new FakeFileSystemScanner(paths), new FakeAudioTagReader(read), NullLogger<CollectionScanner>.Instance);

    private sealed class FakeFileSystemScanner(IReadOnlyList<string> paths) : IFileSystemScanner
    {
        public async IAsyncEnumerable<string> EnumerateAudioFilesAsync(string rootPath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return path;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeAudioTagReader(Func<string, ScanEntry> read) : IAudioTagReader
    {
        public Task<ScanEntry> ReadTagsAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(read(filePath));
    }
}
