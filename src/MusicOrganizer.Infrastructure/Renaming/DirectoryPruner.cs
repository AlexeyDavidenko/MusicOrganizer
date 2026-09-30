using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Renaming;

namespace MusicOrganizer.Infrastructure.Renaming;

/// <summary>
/// Removes empty directories on the local file system. Expected failures (permissions, directory
/// not actually empty, already gone) are reported as <see langword="false"/> instead of throwing,
/// so processing a large collection can continue past a single bad folder.
/// </summary>
public sealed partial class DirectoryPruner : IDirectoryPruner
{
    private readonly ILogger<DirectoryPruner> _logger;

    /// <summary>
    /// Creates a new <see cref="DirectoryPruner"/>.
    /// </summary>
    public DirectoryPruner(ILogger<DirectoryPruner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> TryRemoveIfEmptyAsync(string directory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (!Directory.Exists(directory) || Directory.EnumerateFileSystemEntries(directory).Any())
            {
                return Task.FromResult(false);
            }

            Directory.Delete(directory);
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogPruneFailed(directory, ex);
            return Task.FromResult(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove empty folder {Directory}")]
    private partial void LogPruneFailed(string directory, Exception exception);
}
