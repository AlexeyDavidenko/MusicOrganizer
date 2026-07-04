using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Infrastructure.Deduplication;

/// <summary>
/// Deletes files on the local file system. Expected failures (locked file, permissions, path is
/// actually a directory) are reported as a failed <see cref="FileRemovalResult"/> instead of
/// throwing, so processing a large collection can continue past a single bad file.
/// </summary>
public sealed partial class FileRemover : IFileRemover
{
    private readonly ILogger<FileRemover> _logger;

    /// <summary>
    /// Creates a new <see cref="FileRemover"/>.
    /// </summary>
    public FileRemover(ILogger<FileRemover> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<FileRemovalResult> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            File.Delete(filePath);
            return Task.FromResult(FileRemovalResult.Success(filePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogRemovalFailed(filePath, ex);
            return Task.FromResult(FileRemovalResult.Failure(filePath, ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete {FilePath}")]
    private partial void LogRemovalFailed(string filePath, Exception exception);
}
