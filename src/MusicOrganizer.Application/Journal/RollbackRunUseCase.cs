using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Application.Journal;

/// <summary>
/// Use case: restores every file backed up during a given operation run.
/// </summary>
public sealed partial class RollbackRunUseCase
{
    private readonly IOperationJournal _journal;
    private readonly ILogger<RollbackRunUseCase> _logger;

    /// <summary>
    /// Creates a new <see cref="RollbackRunUseCase"/>.
    /// </summary>
    public RollbackRunUseCase(IOperationJournal journal, ILogger<RollbackRunUseCase> logger)
    {
        _journal = journal;
        _logger = logger;
    }

    /// <summary>
    /// Restores every journal entry recorded for <paramref name="runId"/>, streaming each entry
    /// as it is restored.
    /// </summary>
    /// <param name="runId">Identifier of the operation run to roll back.</param>
    /// <param name="cancellationToken">Token used to stop the rollback early.</param>
    public async IAsyncEnumerable<JournalEntry> RollbackAsync(
        Guid runId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogRollbackStarted(runId);

        await foreach (var entry in _journal.GetEntriesAsync(runId, cancellationToken))
        {
            await _journal.RestoreAsync(entry, cancellationToken);
            yield return entry;
        }

        LogRollbackFinished(runId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting rollback of run {RunId}")]
    private partial void LogRollbackStarted(Guid runId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished rollback of run {RunId}")]
    private partial void LogRollbackFinished(Guid runId);
}
