using System.Runtime.CompilerServices;
using MusicOrganizer.Application.Scanning;

namespace MusicOrganizer.Infrastructure.FileSystem;

/// <summary>
/// Discovers MP3 files on the local file system using lazy directory enumeration, so that
/// collections with millions of files are never loaded into memory at once.
/// </summary>
public sealed class FileSystemScanner : IFileSystemScanner
{
    private const string Mp3SearchPattern = "*.mp3";

    /// <inheritdoc />
    public async IAsyncEnumerable<string> EnumerateAudioFilesAsync(
        string rootPath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var files = Directory.EnumerateFiles(rootPath, Mp3SearchPattern, SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return filePath;

            // Yield the thread periodically so a huge collection doesn't monopolize the
            // caller's async context between file reads.
            await Task.Yield();
        }
    }
}
