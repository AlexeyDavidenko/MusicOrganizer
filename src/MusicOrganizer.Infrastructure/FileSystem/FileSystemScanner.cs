using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Scanning;

namespace MusicOrganizer.Infrastructure.FileSystem;

/// <summary>
/// Discovers MP3 files on the local file system using lazy directory enumeration, so that
/// collections with millions of files are never loaded into memory at once. Walks directories
/// one at a time instead of using <see cref="SearchOption.AllDirectories"/>, so a folder that
/// can't be listed (permissions, or removed mid-scan) is skipped and logged instead of aborting
/// the whole enumeration.
/// </summary>
public sealed partial class FileSystemScanner : IFileSystemScanner
{
    private const string Mp3SearchPattern = "*.mp3";

    private readonly ILogger<FileSystemScanner> _logger;

    /// <summary>
    /// Creates a new <see cref="FileSystemScanner"/>.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public FileSystemScanner(ILogger<FileSystemScanner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> EnumerateAudioFilesAsync(
        string rootPath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pendingDirectories.Pop();

            string[] files;
            string[] subdirectories;
            try
            {
                // Materialized eagerly (bounded by one folder's contents, not the whole
                // collection) because `yield return` isn't allowed inside a try/catch.
                files = Directory.EnumerateFiles(directory, Mp3SearchPattern).ToArray();
                subdirectories = Directory.EnumerateDirectories(directory).ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LogDirectorySkipped(directory, ex);
                continue;
            }

            foreach (var subdirectory in subdirectories)
            {
                pendingDirectories.Push(subdirectory);
            }

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping folder {Directory}: could not list its contents")]
    private partial void LogDirectorySkipped(string directory, Exception exception);
}
