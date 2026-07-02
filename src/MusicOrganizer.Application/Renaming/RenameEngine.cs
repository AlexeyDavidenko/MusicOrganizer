using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Application.Renaming;

/// <summary>
/// Use case: renames every audio file found under a root folder according to the default rename
/// template, resolving name collisions and skipping files without enough tag data to build a name.
/// </summary>
public sealed partial class RenameEngine
{
    private const string OperationType = "rename";
    private const int MaxConflictAttempts = 999;

    private readonly IFileSystemScanner _fileSystemScanner;
    private readonly IAudioTagReader _audioTagReader;
    private readonly IFileRenamer _fileRenamer;
    private readonly IOperationJournal _journal;
    private readonly ILogger<RenameEngine> _logger;

    /// <summary>
    /// Creates a new <see cref="RenameEngine"/>.
    /// </summary>
    public RenameEngine(
        IFileSystemScanner fileSystemScanner,
        IAudioTagReader audioTagReader,
        IFileRenamer fileRenamer,
        IOperationJournal journal,
        ILogger<RenameEngine> logger)
    {
        _fileSystemScanner = fileSystemScanner;
        _audioTagReader = audioTagReader;
        _fileRenamer = fileRenamer;
        _journal = journal;
        _logger = logger;
    }

    /// <summary>
    /// Recursively scans <paramref name="rootPath"/> and streams a <see cref="RenameOutcome"/> per
    /// file found. When <paramref name="dryRun"/> is <see langword="false"/>, a move is recorded
    /// in the journal before each file is actually renamed.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="runId">Identifier for this rename run, used for journaling.</param>
    /// <param name="dryRun">When true, no files are renamed and nothing is journaled.</param>
    /// <param name="cancellationToken">Token used to stop the run early.</param>
    public async IAsyncEnumerable<RenameOutcome> RenameAsync(
        string rootPath,
        Guid runId,
        bool dryRun,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogRenameStarted(rootPath, dryRun);

        await foreach (var filePath in _fileSystemScanner.EnumerateAudioFilesAsync(rootPath, cancellationToken))
        {
            var scanEntry = await _audioTagReader.ReadTagsAsync(filePath, cancellationToken);
            if (!scanEntry.Succeeded)
            {
                yield return new RenameOutcome { OriginalPath = filePath, Error = scanEntry.Error };
                continue;
            }

            var artist = scanEntry.Tags!.Artist;
            var title = scanEntry.Tags.Title;
            if (artist is null || title is null)
            {
                yield return new RenameOutcome { OriginalPath = filePath };
                continue;
            }

            var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            var fileName = DefaultRenameTemplate.BuildFileName(artist, title);
            var proposedPath = Path.Combine(directory, fileName);

            var (resolvedPath, conflictError) = await ResolveConflictAsync(proposedPath, filePath, cancellationToken);
            if (conflictError is not null)
            {
                yield return new RenameOutcome { OriginalPath = filePath, Error = conflictError };
                continue;
            }

            var outcome = new RenameOutcome { OriginalPath = filePath, ProposedPath = resolvedPath };
            if (!outcome.NeedsRename)
            {
                yield return outcome;
                continue;
            }

            if (dryRun)
            {
                yield return outcome;
                continue;
            }

            await _journal.RecordMoveAsync(runId, filePath, resolvedPath!, OperationType, cancellationToken);
            var renameResult = await _fileRenamer.RenameAsync(filePath, resolvedPath!, cancellationToken);

            yield return outcome with { Applied = renameResult.Succeeded, Error = renameResult.Error };
        }

        LogRenameFinished(rootPath);
    }

    private async Task<(string? ResolvedPath, string? Error)> ResolveConflictAsync(
        string proposedPath,
        string originalPath,
        CancellationToken cancellationToken)
    {
        if (string.Equals(proposedPath, originalPath, StringComparison.Ordinal))
        {
            return (proposedPath, null);
        }

        var candidate = proposedPath;
        for (var attempt = 1; attempt <= MaxConflictAttempts; attempt++)
        {
            if (!await _fileRenamer.ExistsAsync(candidate, cancellationToken))
            {
                return (candidate, null);
            }

            candidate = RenameConflictResolver.NextCandidate(proposedPath, attempt);
        }

        return (null, $"Could not find a free name for {proposedPath} after {MaxConflictAttempts} attempts");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting rename of {RootPath} (dryRun={DryRun})")]
    private partial void LogRenameStarted(string rootPath, bool dryRun);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished rename of {RootPath}")]
    private partial void LogRenameFinished(string rootPath);
}
