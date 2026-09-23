using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Application.Renaming;

/// <summary>
/// Use case: moves every audio file found under a root folder into an Artist/Album folder tree
/// (falling back to Artist-only when Album is unknown), keeping each file's current name and
/// resolving name collisions the same way <see cref="RenameEngine"/> does. A file's name is left
/// for <see cref="RenameEngine"/> to manage - run that first if you also want the
/// "Artist-Title.mp3" naming convention applied.
/// </summary>
public sealed partial class OrganizeEngine
{
    private const string OperationType = "organize";

    private readonly IFileSystemScanner _fileSystemScanner;
    private readonly IAudioTagReader _audioTagReader;
    private readonly IFileRenamer _fileRenamer;
    private readonly IOperationJournal _journal;
    private readonly ILogger<OrganizeEngine> _logger;

    /// <summary>
    /// Creates a new <see cref="OrganizeEngine"/>.
    /// </summary>
    public OrganizeEngine(
        IFileSystemScanner fileSystemScanner,
        IAudioTagReader audioTagReader,
        IFileRenamer fileRenamer,
        IOperationJournal journal,
        ILogger<OrganizeEngine> logger)
    {
        _fileSystemScanner = fileSystemScanner;
        _audioTagReader = audioTagReader;
        _fileRenamer = fileRenamer;
        _journal = journal;
        _logger = logger;
    }

    /// <summary>
    /// Recursively scans <paramref name="rootPath"/> and streams a <see cref="RenameOutcome"/> per
    /// file found (its <c>ProposedPath</c> is the target under the Artist/Album tree). When
    /// <paramref name="dryRun"/> is <see langword="false"/>, a move is recorded in the journal
    /// before each file is actually relocated.
    /// </summary>
    /// <param name="rootPath">Root folder to scan, and the root of the Artist/Album tree files are moved under.</param>
    /// <param name="runId">Identifier for this organize run, used for journaling.</param>
    /// <param name="dryRun">When true, no files are moved and nothing is journaled.</param>
    /// <param name="cancellationToken">Token used to stop the run early.</param>
    public async IAsyncEnumerable<RenameOutcome> OrganizeAsync(
        string rootPath,
        Guid runId,
        bool dryRun,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogOrganizeStarted(rootPath, dryRun);

        await foreach (var filePath in _fileSystemScanner.EnumerateAudioFilesAsync(rootPath, cancellationToken))
        {
            var scanEntry = await _audioTagReader.ReadTagsAsync(filePath, cancellationToken);
            if (!scanEntry.Succeeded)
            {
                yield return new RenameOutcome { OriginalPath = filePath, Error = scanEntry.Error };
                continue;
            }

            var artist = scanEntry.Tags!.Artist;
            if (artist is null)
            {
                yield return new RenameOutcome { OriginalPath = filePath };
                continue;
            }

            var targetDirectory = OrganizationTemplate.BuildTargetDirectory(rootPath, artist, scanEntry.Tags.Album);
            var proposedPath = Path.Combine(targetDirectory, Path.GetFileName(filePath));

            var (resolvedPath, conflictError) = await FileRelocationConflictResolver.ResolveAsync(_fileRenamer, proposedPath, filePath, cancellationToken);
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
            var moveResult = await _fileRenamer.RenameAsync(filePath, resolvedPath!, cancellationToken);

            yield return outcome with { Applied = moveResult.Succeeded, Error = moveResult.Error };
        }

        LogOrganizeFinished(rootPath);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting organize of {RootPath} (dryRun={DryRun})")]
    private partial void LogOrganizeStarted(string rootPath, bool dryRun);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished organize of {RootPath}")]
    private partial void LogOrganizeFinished(string rootPath);
}
