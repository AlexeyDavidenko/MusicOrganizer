namespace MusicOrganizer.Domain.Journal;

/// <summary>
/// Outcome of considering a single run for removal from the journal.
/// </summary>
public sealed record JournalCleanupOutcome
{
    /// <summary>Identifier of the run considered.</summary>
    public required Guid RunId { get; init; }

    /// <summary>Timestamp of the run's most recent entry.</summary>
    public required DateTimeOffset LastActivityAtUtc { get; init; }

    /// <summary>Number of entries (backups/move records) the run held.</summary>
    public required int EntryCount { get; init; }

    /// <summary>True when the run was actually deleted from disk.</summary>
    public bool Applied { get; init; }
}
