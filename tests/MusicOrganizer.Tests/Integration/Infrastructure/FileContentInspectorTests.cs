using FluentAssertions;
using MusicOrganizer.Infrastructure.Deduplication;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class FileContentInspectorTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-dedup-").FullName;

    [Fact]
    public async Task ComputeHashAsync_ReturnsTheSameHash_ForFilesWithIdenticalContent()
    {
        var pathA = Path.Combine(_root, "a.mp3");
        var pathB = Path.Combine(_root, "b.mp3");
        await File.WriteAllTextAsync(pathA, "identical content");
        await File.WriteAllTextAsync(pathB, "identical content");

        var sut = new FileContentInspector();

        var hashA = await sut.ComputeHashAsync(pathA);
        var hashB = await sut.ComputeHashAsync(pathB);

        hashA.Should().Be(hashB);
    }

    [Fact]
    public async Task ComputeHashAsync_ReturnsDifferentHashes_ForFilesWithDifferentContent()
    {
        var pathA = Path.Combine(_root, "a.mp3");
        var pathB = Path.Combine(_root, "b.mp3");
        await File.WriteAllTextAsync(pathA, "content A");
        await File.WriteAllTextAsync(pathB, "content B");

        var sut = new FileContentInspector();

        var hashA = await sut.ComputeHashAsync(pathA);
        var hashB = await sut.ComputeHashAsync(pathB);

        hashA.Should().NotBe(hashB);
    }

    [Fact]
    public async Task GetSizeAsync_MatchesTheActualFileSize()
    {
        var path = Path.Combine(_root, "a.mp3");
        await File.WriteAllTextAsync(path, "twelve bytes");

        var sut = new FileContentInspector();

        var size = await sut.GetSizeAsync(path);

        size.Should().Be(new FileInfo(path).Length);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
