using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Application.Deduplication;

/// <summary>
/// Use case: finds duplicate audio files under a root folder, both by exact content match and by
/// matching Artist/Title tags on files with different content.
/// </summary>
public sealed partial class DuplicateFinder
{
    private readonly IFileSystemScanner _fileSystemScanner;
    private readonly IAudioTagReader _audioTagReader;
    private readonly IDuplicateFileInspector _inspector;
    private readonly ILogger<DuplicateFinder> _logger;

    /// <summary>
    /// Creates a new <see cref="DuplicateFinder"/>.
    /// </summary>
    public DuplicateFinder(
        IFileSystemScanner fileSystemScanner,
        IAudioTagReader audioTagReader,
        IDuplicateFileInspector inspector,
        ILogger<DuplicateFinder> logger)
    {
        _fileSystemScanner = fileSystemScanner;
        _audioTagReader = audioTagReader;
        _inspector = inspector;
        _logger = logger;
    }

    /// <summary>
    /// Recursively scans <paramref name="rootPath"/> and returns every group of duplicate files
    /// found, both exact-content and tag-match groups. A group is only known to be complete once
    /// every file has been seen, so unlike other use cases in this project this does not stream
    /// results incrementally.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="cancellationToken">Token used to stop the scan early.</param>
    public async Task<IReadOnlyList<DuplicateGroup>> FindAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        LogScanStarted(rootPath);

        var filePaths = new List<string>();
        await foreach (var path in _fileSystemScanner.EnumerateAudioFilesAsync(rootPath, cancellationToken))
        {
            filePaths.Add(path);
        }

        var exactGroups = await FindExactDuplicatesAsync(filePaths, cancellationToken);
        var tagMatchGroups = await FindTagMatchDuplicatesAsync(filePaths, cancellationToken);
        var groups = exactGroups.Concat(tagMatchGroups).ToList();

        LogScanFinished(rootPath, groups.Count);
        return groups;
    }

    private async Task<List<DuplicateGroup>> FindExactDuplicatesAsync(List<string> filePaths, CancellationToken cancellationToken)
    {
        var bySize = new Dictionary<long, List<string>>();
        foreach (var path in filePaths)
        {
            long size;
            try
            {
                size = await _inspector.GetSizeAsync(path, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LogProbeFailed(path, ex);
                continue;
            }

            AddTo(bySize, size, path);
        }

        var groups = new List<DuplicateGroup>();
        foreach (var (size, pathsOfSize) in bySize)
        {
            if (pathsOfSize.Count < 2)
            {
                continue;
            }

            var byHash = new Dictionary<string, List<string>>();
            foreach (var path in pathsOfSize)
            {
                string hash;
                try
                {
                    hash = await _inspector.ComputeHashAsync(path, cancellationToken);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    LogProbeFailed(path, ex);
                    continue;
                }

                AddTo(byHash, hash, path);
            }

            foreach (var (hash, pathsOfHash) in byHash)
            {
                if (pathsOfHash.Count >= 2)
                {
                    groups.Add(new DuplicateGroup(DuplicateMatchKind.Exact, hash, pathsOfHash, size));
                }
            }
        }

        return groups;
    }

    private async Task<List<DuplicateGroup>> FindTagMatchDuplicatesAsync(List<string> filePaths, CancellationToken cancellationToken)
    {
        var byKey = new Dictionary<string, List<string>>();
        foreach (var path in filePaths)
        {
            var scanEntry = await _audioTagReader.ReadTagsAsync(path, cancellationToken);
            if (!scanEntry.Succeeded || scanEntry.Tags!.Artist is null || scanEntry.Tags.Title is null)
            {
                continue;
            }

            var key = TagMatchKey.Build(scanEntry.Tags.Artist, scanEntry.Tags.Title);
            AddTo(byKey, key, path);
        }

        var groups = new List<DuplicateGroup>();
        foreach (var (key, pathsOfKey) in byKey)
        {
            if (pathsOfKey.Count >= 2)
            {
                groups.Add(new DuplicateGroup(DuplicateMatchKind.TagMatch, key, pathsOfKey, null));
            }
        }

        return groups;
    }

    private static void AddTo<TKey>(Dictionary<TKey, List<string>> map, TKey key, string path)
        where TKey : notnull
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }

        list.Add(path);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting duplicate scan of {RootPath}")]
    private partial void LogScanStarted(string rootPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished duplicate scan of {RootPath}: {GroupCount} group(s) found")]
    private partial void LogScanFinished(string rootPath, int groupCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to probe {FilePath} for duplicate detection")]
    private partial void LogProbeFailed(string filePath, Exception exception);
}
