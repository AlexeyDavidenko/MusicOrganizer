using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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

        var sut = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);

        var found = new List<string>();
        await foreach (var path in sut.EnumerateAudioFilesAsync(_root))
        {
            found.Add(path);
        }

        found.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task EnumerateAudioFilesAsync_SkipsDirectoryThatBecomesUnreadable_AndStillFindsSiblingFiles()
    {
        // "bad" is discovered as a subdirectory but removed before the scanner gets to list its
        // contents (triggered from inside the loop below, right after the sibling file at the
        // root is yielded) - a deterministic, cross-platform way to force EnumerateDirectories to
        // throw for one specific folder without relying on OS permission semantics (which behave
        // inconsistently for a root/admin test runner).
        var badDirectory = Directory.CreateDirectory(Path.Combine(_root, "bad")).FullName;
        var goodDirectory = Directory.CreateDirectory(Path.Combine(_root, "good")).FullName;
        var triggerPath = Path.Combine(_root, "trigger.mp3");
        var keepPath = Path.Combine(goodDirectory, "keep.mp3");
        File.WriteAllBytes(triggerPath, []);
        File.WriteAllBytes(keepPath, []);

        var sut = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);

        var found = new List<string>();
        await foreach (var path in sut.EnumerateAudioFilesAsync(_root))
        {
            found.Add(path);
            if (path == triggerPath)
            {
                Directory.Delete(badDirectory, recursive: true);
            }
        }

        found.Should().BeEquivalentTo([triggerPath, keepPath]);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
