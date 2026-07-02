using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Infrastructure.Renaming;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class FileRenamerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-rename-").FullName;

    [Fact]
    public async Task RenameAsync_MovesTheFileToTheNewPath()
    {
        var originalPath = Path.Combine(_root, "original.mp3");
        var newPath = Path.Combine(_root, "Artist-Title.mp3");
        await File.WriteAllTextAsync(originalPath, "content");

        var sut = new FileRenamer(NullLogger<FileRenamer>.Instance);

        var result = await sut.RenameAsync(originalPath, newPath);

        result.Succeeded.Should().BeTrue();
        File.Exists(originalPath).Should().BeFalse();
        File.Exists(newPath).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReflectsWhetherAFileIsPresent()
    {
        var path = Path.Combine(_root, "present.mp3");
        await File.WriteAllTextAsync(path, "content");

        var sut = new FileRenamer(NullLogger<FileRenamer>.Instance);

        (await sut.ExistsAsync(path)).Should().BeTrue();
        (await sut.ExistsAsync(Path.Combine(_root, "missing.mp3"))).Should().BeFalse();
    }

    [Fact]
    public async Task RenameAsync_ReturnsFailure_WhenSourceFileDoesNotExist()
    {
        var sut = new FileRenamer(NullLogger<FileRenamer>.Instance);

        var result = await sut.RenameAsync(
            Path.Combine(_root, "missing.mp3"),
            Path.Combine(_root, "target.mp3"));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
