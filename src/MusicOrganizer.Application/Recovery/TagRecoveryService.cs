using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Application.Recovery;

/// <summary>
/// Use case: recovers missing tags for every audio file found under a root folder.
/// </summary>
public sealed partial class TagRecoveryService
{
    private const string OperationType = "tag-recovery";

    private readonly IFileSystemScanner _fileSystemScanner;
    private readonly IAudioTagReader _audioTagReader;
    private readonly ITagWriter _tagWriter;
    private readonly IOperationJournal _journal;
    private readonly ILogger<TagRecoveryService> _logger;

    /// <summary>
    /// Creates a new <see cref="TagRecoveryService"/>.
    /// </summary>
    public TagRecoveryService(
        IFileSystemScanner fileSystemScanner,
        IAudioTagReader audioTagReader,
        ITagWriter tagWriter,
        IOperationJournal journal,
        ILogger<TagRecoveryService> logger)
    {
        _fileSystemScanner = fileSystemScanner;
        _audioTagReader = audioTagReader;
        _tagWriter = tagWriter;
        _journal = journal;
        _logger = logger;
    }

    /// <summary>
    /// Recursively scans <paramref name="rootPath"/> and streams a <see cref="TagRecoveryOutcome"/>
    /// per file found. When <paramref name="dryRun"/> is <see langword="false"/>, a backup is
    /// recorded before each file is actually written to.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="runId">Identifier for this recovery run, used for journaling.</param>
    /// <param name="dryRun">When true, no files are modified and nothing is journaled.</param>
    /// <param name="cancellationToken">Token used to stop the run early.</param>
    public async IAsyncEnumerable<TagRecoveryOutcome> RecoverAsync(
        string rootPath,
        Guid runId,
        bool dryRun,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogRecoveryStarted(rootPath, dryRun);

        await foreach (var filePath in _fileSystemScanner.EnumerateAudioFilesAsync(rootPath, cancellationToken))
        {
            var scanEntry = await _audioTagReader.ReadTagsAsync(filePath, cancellationToken);
            if (!scanEntry.Succeeded)
            {
                yield return new TagRecoveryOutcome { FilePath = filePath, Error = scanEntry.Error };
                continue;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var proposal = FilenameTagRecovery.Propose(fileNameWithoutExtension, scanEntry.Tags!);

            if (proposal.RecoveredFields.Count == 0)
            {
                yield return new TagRecoveryOutcome { FilePath = filePath };
                continue;
            }

            if (dryRun)
            {
                yield return new TagRecoveryOutcome
                {
                    FilePath = filePath,
                    RecoveredFields = proposal.RecoveredFields,
                    Applied = false,
                };
                continue;
            }

            await _journal.RecordMutationAsync(runId, filePath, OperationType, cancellationToken);
            var writeResult = await _tagWriter.WriteTagsAsync(filePath, proposal.MergedTags, cancellationToken);

            yield return new TagRecoveryOutcome
            {
                FilePath = filePath,
                RecoveredFields = proposal.RecoveredFields,
                Applied = writeResult.Succeeded,
                Error = writeResult.Error,
            };
        }

        LogRecoveryFinished(rootPath);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting tag recovery of {RootPath} (dryRun={DryRun})")]
    private partial void LogRecoveryStarted(string rootPath, bool dryRun);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished tag recovery of {RootPath}")]
    private partial void LogRecoveryFinished(string rootPath);
}
