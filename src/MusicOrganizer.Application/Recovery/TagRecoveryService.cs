using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Application.Recovery;

/// <summary>
/// Use case: recovers missing tags for every audio file found under a root folder, by running
/// each <see cref="ITagRecoverySource"/> in priority order (registration order in DI).
/// </summary>
public sealed partial class TagRecoveryService
{
    private const string OperationType = "tag-recovery";

    private readonly IFileSystemScanner _fileSystemScanner;
    private readonly IAudioTagReader _audioTagReader;
    private readonly ITagWriter _tagWriter;
    private readonly IOperationJournal _journal;
    private readonly IReadOnlyList<ITagRecoverySource> _sources;
    private readonly ILogger<TagRecoveryService> _logger;

    /// <summary>
    /// Creates a new <see cref="TagRecoveryService"/>.
    /// </summary>
    public TagRecoveryService(
        IFileSystemScanner fileSystemScanner,
        IAudioTagReader audioTagReader,
        ITagWriter tagWriter,
        IOperationJournal journal,
        IEnumerable<ITagRecoverySource> sources,
        ILogger<TagRecoveryService> logger)
    {
        _fileSystemScanner = fileSystemScanner;
        _audioTagReader = audioTagReader;
        _tagWriter = tagWriter;
        _journal = journal;
        _sources = sources.ToArray();
        _logger = logger;
    }

    /// <summary>
    /// Recursively scans <paramref name="rootPath"/> and streams a <see cref="TagRecoveryOutcome"/>
    /// per file found. When <paramref name="dryRun"/> is <see langword="false"/>, a backup is
    /// recorded before each file is actually written to.
    /// </summary>
    /// <remarks>
    /// Runs in two phases rather than a single streaming pass: TAG RECOVERY level 6 (Existing
    /// Collection Statistics) needs every sibling's tags before it can propose anything for a
    /// given file, so every file's tags are read once upfront to build that context, then reused
    /// (not re-read) while processing each file. This is a conscious exception to the project's
    /// streaming-first principle — the same one <c>DuplicateFinder</c> already makes, for the same
    /// underlying reason (see ADR-0004, ADR-0008).
    /// </remarks>
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

        var filePaths = new List<string>();
        await foreach (var path in _fileSystemScanner.EnumerateAudioFilesAsync(rootPath, cancellationToken))
        {
            filePaths.Add(path);
        }

        var scanEntries = new Dictionary<string, ScanEntry>();
        foreach (var filePath in filePaths)
        {
            scanEntries[filePath] = await _audioTagReader.ReadTagsAsync(filePath, cancellationToken);
        }

        var successfulTags = scanEntries
            .Where(kv => kv.Value.Succeeded)
            .ToDictionary(kv => kv.Key, kv => kv.Value.Tags!);
        var context = CollectionStatisticsTagRecovery.Build(successfulTags);

        foreach (var filePath in filePaths)
        {
            var scanEntry = scanEntries[filePath];
            if (!scanEntry.Succeeded)
            {
                yield return new TagRecoveryOutcome { FilePath = filePath, Error = scanEntry.Error };
                continue;
            }

            var proposal = Propose(filePath, rootPath, scanEntry.Tags!, context);
            var needsManualReview = proposal.MergedTags.Artist is null || proposal.MergedTags.Title is null;

            if (proposal.RecoveredFields.Count == 0)
            {
                yield return new TagRecoveryOutcome { FilePath = filePath, NeedsManualReview = needsManualReview };
                continue;
            }

            if (dryRun)
            {
                yield return new TagRecoveryOutcome
                {
                    FilePath = filePath,
                    RecoveredFields = proposal.RecoveredFields,
                    Applied = false,
                    NeedsManualReview = needsManualReview,
                };
                continue;
            }

            await _journal.RecordMutationAsync(runId, filePath, OperationType, cancellationToken);
            var writeResult = await _tagWriter.WriteTagsAsync(filePath, proposal.MergedTags, cancellationToken);
            var error = writeResult.Succeeded ? null : writeResult.Error;

            yield return new TagRecoveryOutcome
            {
                FilePath = filePath,
                RecoveredFields = proposal.RecoveredFields,
                Applied = writeResult.Succeeded,
                Error = error,
                NeedsManualReview = error is null && needsManualReview,
            };
        }

        LogRecoveryFinished(rootPath);
    }

    private TagRecoveryProposal Propose(string filePath, string rootPath, AudioTags tags, TagRecoveryContext context)
    {
        var merged = tags;
        var recoveredFields = new List<string>();

        foreach (var source in _sources)
        {
            var proposal = source.Propose(filePath, rootPath, merged, context);
            merged = proposal.MergedTags;
            recoveredFields.AddRange(proposal.RecoveredFields);
        }

        return new TagRecoveryProposal(merged, recoveredFields);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting tag recovery of {RootPath} (dryRun={DryRun})")]
    private partial void LogRecoveryStarted(string rootPath, bool dryRun);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished tag recovery of {RootPath}")]
    private partial void LogRecoveryFinished(string rootPath);
}
