using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Infrastructure.Deduplication;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class FileRemoverTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-remove-").FullName;

    [Fact]
    public async Task DeleteAsync_DeletesAnExistingFile()
    {
        var path = Path.Combine(_root, "duplicate.mp3");
        await File.WriteAllTextAsync(path, "content");

        var sut = new FileRemover(NullLogger<FileRemover>.Instance);

        var result = await sut.DeleteAsync(path);

        result.Succeeded.Should().BeTrue();
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailure_WhenPathIsADirectoryNotAFile()
    {
        var directoryPath = Path.Combine(_root, "not-a-file");
        Directory.CreateDirectory(directoryPath);

        var sut = new FileRemover(NullLogger<FileRemover>.Instance);

        var result = await sut.DeleteAsync(directoryPath);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
