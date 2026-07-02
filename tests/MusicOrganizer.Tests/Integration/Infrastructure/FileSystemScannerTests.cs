using FluentAssertions;
using MusicOrganizer.Infrastructure.FileSystem;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class FileSystemScannerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-scan-").FullName;

    [Fact]
    public async Task EnumerateAudioFilesAsync_FindsMp3FilesRecursively_AndIgnoresOtherExtensions()
    {
        var subFolder = Directory.CreateDirectory(Path.Combine(_root, "sub")).FullName;

        var expected = new[]
        {
            Path.Combine(_root, "a.mp3"),
            Path.Combine(subFolder, "b.mp3"),
        };

        foreach (var path in expected)
        {
            File.WriteAllBytes(path, []);
        }

        File.WriteAllText(Path.Combine(subFolder, "notes.txt"), "not audio");

        var sut = new FileSystemScanner();

        var found = new List<string>();
        await foreach (var path in sut.EnumerateAudioFilesAsync(_root))
        {
            found.Add(path);
        }

        found.Should().BeEquivalentTo(expected);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
