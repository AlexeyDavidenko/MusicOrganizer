using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Tests.Unit.Application;

public class DuplicateFinderTests
{
    [Fact]
    public async Task FindAsync_GroupsFilesWithTheSameSizeAndHash_AsExactDuplicates()
    {
        var scanner = new FakeFileSystemScanner(["a.mp3", "b.mp3"]);
        var reader = new FakeAudioTagReader(_ => EmptyTags());
        var inspector = new FakeInspector(
            size: _ => 100,
            hash: _ => "same-hash");
        var sut = new DuplicateFinder(scanner, reader, inspector, NullLogger<DuplicateFinder>.Instance);

        var groups = await sut.FindAsync("root");

        groups.Should().ContainSingle(g => g.Kind == DuplicateMatchKind.Exact);
        var group = groups.Single(g => g.Kind == DuplicateMatchKind.Exact);
        group.FilePaths.Should().BeEquivalentTo(["a.mp3", "b.mp3"]);
        group.FileSize.Should().Be(100);
    }

    [Fact]
    public async Task FindAsync_NeverHashesAFile_WhoseSizeIsUnique()
    {
        var scanner = new FakeFileSystemScanner(["unique.mp3", "a.mp3", "b.mp3"]);
        var reader = new FakeAudioTagReader(_ => EmptyTags());
        var inspector = new FakeInspector(
            size: path => path == "unique.mp3" ? 999 : 100,
            hash: _ => "same-hash");
        var sut = new DuplicateFinder(scanner, reader, inspector, NullLogger<DuplicateFinder>.Instance);

        await sut.FindAsync("root");

        inspector.HashedPaths.Should().NotContain("unique.mp3");
        inspector.HashedPaths.Should().Contain(["a.mp3", "b.mp3"]);
    }

    [Fact]
    public async Task FindAsync_DoesNotGroupFiles_WithSameSizeButDifferentHash()
    {
        var scanner = new FakeFileSystemScanner(["a.mp3", "b.mp3"]);
        var reader = new FakeAudioTagReader(_ => EmptyTags());
        var inspector = new FakeInspector(
            size: _ => 100,
            hash: path => path == "a.mp3" ? "hash-a" : "hash-b");
        var sut = new DuplicateFinder(scanner, reader, inspector, NullLogger<DuplicateFinder>.Instance);

        var groups = await sut.FindAsync("root");

        groups.Should().NotContain(g => g.Kind == DuplicateMatchKind.Exact);
    }

    [Fact]
    public async Task FindAsync_GroupsFilesWithMatchingTags_AsTagMatchDuplicates_EvenWithDifferentContent()
    {
        var scanner = new FakeFileSystemScanner(["a.mp3", "b.mp3"]);
        var reader = new FakeAudioTagReader(_ => TagsFor("Artist", "Title"));
        var inspector = new FakeInspector(
            size: path => path == "a.mp3" ? 100 : 200,
            hash: _ => "irrelevant");
        var sut = new DuplicateFinder(scanner, reader, inspector, NullLogger<DuplicateFinder>.Instance);

        var groups = await sut.FindAsync("root");

        groups.Should().ContainSingle(g => g.Kind == DuplicateMatchKind.TagMatch);
        var group = groups.Single(g => g.Kind == DuplicateMatchKind.TagMatch);
        group.FilePaths.Should().BeEquivalentTo(["a.mp3", "b.mp3"]);
        group.FileSize.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_NeverGroupsFiles_MissingArtistOrTitle()
    {
        var scanner = new FakeFileSystemScanner(["a.mp3", "b.mp3"]);
        var reader = new FakeAudioTagReader(_ => TagsFor(null, "Title"));
        var inspector = new FakeInspector(size: _ => 100, hash: _ => "irrelevant");
        var sut = new DuplicateFinder(scanner, reader, inspector, NullLogger<DuplicateFinder>.Instance);

        var groups = await sut.FindAsync("root");

        groups.Should().NotContain(g => g.Kind == DuplicateMatchKind.TagMatch);
    }

    [Fact]
    public async Task FindAsync_ContinuesPastAProbeFailure_OnOtherFiles()
    {
        var scanner = new FakeFileSystemScanner(["broken.mp3", "a.mp3", "b.mp3"]);
        var reader = new FakeAudioTagReader(_ => EmptyTags());
        var inspector = new FakeInspector(
            size: path => path == "broken.mp3" ? throw new IOException("disk error") : 100,
            hash: _ => "same-hash");
        var sut = new DuplicateFinder(scanner, reader, inspector, NullLogger<DuplicateFinder>.Instance);

        var groups = await sut.FindAsync("root");

        groups.Should().ContainSingle(g => g.Kind == DuplicateMatchKind.Exact);
    }

    private static AudioTags EmptyTags() => new(Title: null, Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);

    private static AudioTags TagsFor(string? artist, string? title) =>
        new(Title: title, Artist: artist, Album: null, Year: null, TrackNumber: null, Genre: null);

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

    private sealed class FakeAudioTagReader(Func<string, AudioTags> tagsFactory) : IAudioTagReader
    {
        public Task<ScanEntry> ReadTagsAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(ScanEntry.Success(filePath, tagsFactory(filePath)));
    }

    private sealed class FakeInspector(Func<string, long> size, Func<string, string> hash) : IDuplicateFileInspector
    {
        public List<string> HashedPaths { get; } = [];

        public Task<long> GetSizeAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(size(filePath));

        public Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
        {
            HashedPaths.Add(filePath);
            return Task.FromResult(hash(filePath));
        }
    }
}
