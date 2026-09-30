using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Application.Journal;

/// <summary>
/// Port for inspecting and pruning the journal store itself, as opposed to
/// <see cref="IOperationJournal"/>'s per-operation record/restore concerns. Used by the
/// <c>clean-journal</c> command, not by any mutating engine.
/// </summary>
public interface IJournalMaintenance
{
    /// <summary>
    /// Streams a summary of every operation run currently recorded in the journal.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    public IAsyncEnumerable<JournalRunSummary> GetAllRunsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a run's backups and manifest. Not reversible - once deleted, that
    /// run's own restore points are gone (the files/directories it points at, if unmodified since,
    /// remain on disk; the ability to <c>rollback</c> that specific run does not).
    /// </summary>
    /// <param name="runId">Identifier of the run to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task DeleteRunAsync(Guid runId, CancellationToken cancellationToken = default);
}
