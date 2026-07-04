using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Application.Deduplication;

/// <summary>
/// Use case: removes exact-duplicate files under a root folder, keeping one file per group (see
/// <see cref="DuplicateKeeperSelector"/>) and deleting the rest. Tag-match groups (matching tags,
/// different content) are never touched — that signal is too low-confidence to act on
/// automatically.
/// </summary>
public sealed partial class DuplicateRemovalService
{
    private const string OperationType = "duplicate-removal";

    private readonly IDuplicateFinder _duplicateFinder;
    private readonly IFileRemover _fileRemover;
    private readonly IOperationJournal _journal;
    private readonly ILogger<DuplicateRemovalService> _logger;

    /// <summary>
    /// Creates a new <see cref="DuplicateRemovalService"/>.
    /// </summary>
    public DuplicateRemovalService(
        IDuplicateFinder duplicateFinder,
        IFileRemover fileRemover,
        IOperationJournal journal,
        ILogger<DuplicateRemovalService> logger)
    {
        _duplicateFinder = duplicateFinder;
        _fileRemover = fileRemover;
        _journal = journal;
        _logger = logger;
    }

    /// <summary>
    /// Finds exact-duplicate groups under <paramref name="rootPath"/> and streams a
    /// <see cref="DuplicateRemovalOutcome"/> per file removed (or, in a dry run, that would be
    /// removed). A failure on one file never stops the rest.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="runId">Identifier for this removal run, used for journaling.</param>
    /// <param name="dryRun">When true, no files are deleted and nothing is journaled.</param>
    /// <param name="cancellationToken">Token used to stop the run early.</param>
    public async IAsyncEnumerable<DuplicateRemovalOutcome> RemoveAsync(
        string rootPath,
        Guid runId,
        bool dryRun,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogRemovalStarted(rootPath, dryRun);

        var groups = await _duplicateFinder.FindAsync(rootPath, cancellationToken);

        foreach (var group in groups.Where(g => g.Kind == DuplicateMatchKind.Exact))
        {
            var keeper = DuplicateKeeperSelector.SelectKeeper(group.FilePaths);

            foreach (var filePath in group.FilePaths)
            {
                if (filePath == keeper)
                {
                    continue;
                }

                if (dryRun)
                {
                    yield return new DuplicateRemovalOutcome { FilePath = filePath, KeptFilePath = keeper };
                    continue;
                }

                await _journal.RecordMutationAsync(runId, filePath, OperationType, cancellationToken);
                var result = await _fileRemover.DeleteAsync(filePath, cancellationToken);

                yield return new DuplicateRemovalOutcome
                {
                    FilePath = filePath,
                    KeptFilePath = keeper,
                    Applied = result.Succeeded,
                    Error = result.Error,
                };
            }
        }

        LogRemovalFinished(rootPath);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting duplicate removal in {RootPath} (dryRun={DryRun})")]
    private partial void LogRemovalStarted(string rootPath, bool dryRun);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished duplicate removal in {RootPath}")]
    private partial void LogRemovalFinished(string rootPath);
}
