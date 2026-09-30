using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Application.Journal;

/// <summary>
/// Use case: deletes journal runs whose most recent activity is older than a cutoff, so backups
/// don't accumulate under the journal root forever (see ADR-0002, Consequences).
/// </summary>
public sealed partial class CleanJournalUseCase
{
    private readonly IJournalMaintenance _journalMaintenance;
    private readonly ILogger<CleanJournalUseCase> _logger;

    /// <summary>
    /// Creates a new <see cref="CleanJournalUseCase"/>.
    /// </summary>
    public CleanJournalUseCase(IJournalMaintenance journalMaintenance, ILogger<CleanJournalUseCase> logger)
    {
        _journalMaintenance = journalMaintenance;
        _logger = logger;
    }

    /// <summary>
    /// Streams a <see cref="JournalCleanupOutcome"/> for every run whose most recent entry is
    /// older than <paramref name="olderThan"/>. When <paramref name="dryRun"/> is
    /// <see langword="false"/>, each such run is deleted as it's reported.
    /// </summary>
    /// <param name="olderThan">Minimum age (relative to now) a run must have to be pruned.</param>
    /// <param name="dryRun">When true, no runs are deleted.</param>
    /// <param name="cancellationToken">Token used to stop the run early.</param>
    public async IAsyncEnumerable<JournalCleanupOutcome> CleanAsync(
        TimeSpan olderThan,
        bool dryRun,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogCleanupStarted(olderThan, dryRun);
        var cutoff = DateTimeOffset.UtcNow - olderThan;

        await foreach (var run in _journalMaintenance.GetAllRunsAsync(cancellationToken))
        {
            if (run.LastActivityAtUtc >= cutoff)
            {
                continue;
            }

            if (!dryRun)
            {
                await _journalMaintenance.DeleteRunAsync(run.RunId, cancellationToken);
            }

            yield return new JournalCleanupOutcome
            {
                RunId = run.RunId,
                LastActivityAtUtc = run.LastActivityAtUtc,
                EntryCount = run.EntryCount,
                Applied = !dryRun,
            };
        }

        LogCleanupFinished();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting journal cleanup (olderThan={OlderThan}, dryRun={DryRun})")]
    private partial void LogCleanupStarted(TimeSpan olderThan, bool dryRun);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished journal cleanup")]
    private partial void LogCleanupFinished();
}
