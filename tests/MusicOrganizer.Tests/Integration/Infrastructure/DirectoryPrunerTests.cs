using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Infrastructure.Renaming;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class DirectoryPrunerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-prune-").FullName;

    [Fact]
    public async Task TryRemoveIfEmptyAsync_RemovesAndReturnsTrue_ForAnEmptyDirectory()
    {
        var directory = Directory.CreateDirectory(Path.Combine(_root, "empty")).FullName;
        var sut = new DirectoryPruner(NullLogger<DirectoryPruner>.Instance);

        var removed = await sut.TryRemoveIfEmptyAsync(directory);

        removed.Should().BeTrue();
        Directory.Exists(directory).Should().BeFalse();
    }

    [Fact]
    public async Task TryRemoveIfEmptyAsync_ReturnsFalseAndDoesNotDelete_ForADirectoryContainingAFile()
    {
        var directory = Directory.CreateDirectory(Path.Combine(_root, "not-empty")).FullName;
        await File.WriteAllTextAsync(Path.Combine(directory, "track.mp3"), "content");
        var sut = new DirectoryPruner(NullLogger<DirectoryPruner>.Instance);

        var removed = await sut.TryRemoveIfEmptyAsync(directory);

        removed.Should().BeFalse();
        Directory.Exists(directory).Should().BeTrue();
    }

    [Fact]
    public async Task TryRemoveIfEmptyAsync_ReturnsFalseAndDoesNotDelete_ForADirectoryContainingAnEmptySubdirectory()
    {
        var directory = Directory.CreateDirectory(Path.Combine(_root, "parent")).FullName;
        Directory.CreateDirectory(Path.Combine(directory, "child"));
        var sut = new DirectoryPruner(NullLogger<DirectoryPruner>.Instance);

        var removed = await sut.TryRemoveIfEmptyAsync(directory);

        removed.Should().BeFalse();
        Directory.Exists(directory).Should().BeTrue();
    }

    [Fact]
    public async Task TryRemoveIfEmptyAsync_ReturnsFalse_ForADirectoryThatDoesNotExist()
    {
        var sut = new DirectoryPruner(NullLogger<DirectoryPruner>.Instance);

        var removed = await sut.TryRemoveIfEmptyAsync(Path.Combine(_root, "missing"));

        removed.Should().BeFalse();
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
