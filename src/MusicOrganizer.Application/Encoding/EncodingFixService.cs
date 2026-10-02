using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain.Encoding;

namespace MusicOrganizer.Application.Encoding;

/// <summary>
/// Use case: corrects mis-decoded text tags (Cyrillic legacy encodings and Windows-1252
/// punctuation read as Latin1 - see PROMPT.md, "MP3 SUPPORT") for every audio file found under a
/// root folder.
/// </summary>
public sealed partial class EncodingFixService
{
    private const string OperationType = "fix-encoding";

    private readonly IFileSystemScanner _fileSystemScanner;
    private readonly IAudioTagReader _audioTagReader;
    private readonly IEncodingEligibilityInspector _eligibilityInspector;
    private readonly ITagWriter _tagWriter;
    private readonly IOperationJournal _journal;
    private readonly ILogger<EncodingFixService> _logger;

    /// <summary>
    /// Creates a new <see cref="EncodingFixService"/>.
    /// </summary>
    /// <param name="fileSystemScanner">Enumerates audio files under a root folder.</param>
    /// <param name="audioTagReader">Reads a file's current tags.</param>
    /// <param name="eligibilityInspector">Determines which tag fields are eligible for the fix.</param>
    /// <param name="tagWriter">Writes corrected tags back to a file.</param>
    /// <param name="journal">Records a backup before each file is mutated.</param>
    /// <param name="logger">Logger.</param>
    public EncodingFixService(
        IFileSystemScanner fileSystemScanner,
        IAudioTagReader audioTagReader,
        IEncodingEligibilityInspector eligibilityInspector,
        ITagWriter tagWriter,
        IOperationJournal journal,
        ILogger<EncodingFixService> logger)
    {
        _fileSystemScanner = fileSystemScanner;
        _audioTagReader = audioTagReader;
        _eligibilityInspector = eligibilityInspector;
        _tagWriter = tagWriter;
        _journal = journal;
        _logger = logger;
    }

    /// <summary>
    /// Recursively scans <paramref name="rootPath"/> and streams an <see cref="EncodingFixOutcome"/>
    /// per file found. When <paramref name="dryRun"/> is <see langword="false"/>, a backup is
    /// recorded before each file is actually written to.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="runId">Identifier for this run, used for journaling.</param>
    /// <param name="dryRun">When true, no files are modified and nothing is journaled.</param>
    /// <param name="cancellationToken">Token used to stop the run early.</param>
    /// <returns>One outcome per file scanned.</returns>
    public async IAsyncEnumerable<EncodingFixOutcome> FixAsync(
        string rootPath,
        Guid runId,
        bool dryRun,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogFixStarted(rootPath, dryRun);

        await foreach (var filePath in _fileSystemScanner.EnumerateAudioFilesAsync(rootPath, cancellationToken))
        {
            var scanEntry = await _audioTagReader.ReadTagsAsync(filePath, cancellationToken);
            if (!scanEntry.Succeeded)
            {
                yield return new EncodingFixOutcome { FilePath = filePath, Error = scanEntry.Error };
                continue;
            }

            var eligibility = await _eligibilityInspector.InspectAsync(filePath, cancellationToken);
            var proposal = AudioTagsEncodingFixer.Propose(scanEntry.Tags!, eligibility);

            if (proposal.FixedFields.Count == 0)
            {
                yield return new EncodingFixOutcome { FilePath = filePath, NeedsManualReview = proposal.NeedsManualReview };
                continue;
            }

            if (dryRun)
            {
                yield return new EncodingFixOutcome
                {
                    FilePath = filePath,
                    FixedFields = proposal.FixedFields,
                    NeedsManualReview = proposal.NeedsManualReview,
                };
                continue;
            }

            await _journal.RecordMutationAsync(runId, filePath, OperationType, cancellationToken);
            var writeResult = await _tagWriter.WriteTagsAsync(filePath, proposal.FixedTags, cancellationToken);

            yield return new EncodingFixOutcome
            {
                FilePath = filePath,
                FixedFields = proposal.FixedFields,
                Applied = writeResult.Succeeded,
                Error = writeResult.Succeeded ? null : writeResult.Error,
                NeedsManualReview = proposal.NeedsManualReview,
            };
        }

        LogFixFinished(rootPath);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting encoding fix of {RootPath} (dryRun={DryRun})")]
    private partial void LogFixStarted(string rootPath, bool dryRun);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished encoding fix of {RootPath}")]
    private partial void LogFixFinished(string rootPath);
}
