using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Domain;

namespace MusicOrganizer.Application.Scanning;

/// <summary>
/// Use case: streams the tags of every audio file found under a root folder.
/// </summary>
public sealed partial class CollectionScanner
{
    private readonly IFileSystemScanner _fileSystemScanner;
    private readonly IAudioTagReader _audioTagReader;
    private readonly ILogger<CollectionScanner> _logger;

    /// <summary>
    /// Creates a new <see cref="CollectionScanner"/>.
    /// </summary>
    public CollectionScanner(
        IFileSystemScanner fileSystemScanner,
        IAudioTagReader audioTagReader,
        ILogger<CollectionScanner> logger)
    {
        _fileSystemScanner = fileSystemScanner;
        _audioTagReader = audioTagReader;
        _logger = logger;
    }

    /// <summary>
    /// Recursively scans <paramref name="rootPath"/> and streams a <see cref="ScanEntry"/> per
    /// file found. Files are yielded one at a time; the whole collection is never buffered in
    /// memory.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="cancellationToken">Token used to stop the scan early.</param>
    public async IAsyncEnumerable<ScanEntry> ScanAsync(
        string rootPath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogScanStarted(rootPath);

        await foreach (var filePath in _fileSystemScanner.EnumerateAudioFilesAsync(rootPath, cancellationToken))
        {
            yield return await _audioTagReader.ReadTagsAsync(filePath, cancellationToken);
        }

        LogScanFinished(rootPath);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting scan of {RootPath}")]
    private partial void LogScanStarted(string rootPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished scan of {RootPath}")]
    private partial void LogScanFinished(string rootPath);
}
