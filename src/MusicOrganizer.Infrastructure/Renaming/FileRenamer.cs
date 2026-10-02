using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Renaming;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Infrastructure.Renaming;

/// <summary>
/// Renames (or moves - the two are the same file system operation) files on the local file
/// system, creating the destination folder first if needed. Expected failures (locked file,
/// permissions) are reported as a failed <see cref="RenameResult"/> instead of throwing, so
/// processing a large collection can continue past a single bad file.
/// </summary>
public sealed partial class FileRenamer : IFileRenamer
{
    private readonly ILogger<FileRenamer> _logger;

    /// <summary>
    /// Creates a new <see cref="FileRenamer"/>.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public FileRenamer(ILogger<FileRenamer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(path));
    }

    /// <inheritdoc />
    public Task<RenameResult> RenameAsync(string originalPath, string newPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var targetDirectory = Path.GetDirectoryName(newPath);
            if (!string.IsNullOrEmpty(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Move(originalPath, newPath);
            return Task.FromResult(RenameResult.Success(originalPath, newPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogRenameFailed(originalPath, newPath, ex);
            return Task.FromResult(RenameResult.Failure(originalPath, ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to rename {OriginalPath} to {NewPath}")]
    private partial void LogRenameFailed(string originalPath, string newPath, Exception exception);
}
