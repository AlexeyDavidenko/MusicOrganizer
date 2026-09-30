namespace MusicOrganizer.Domain.Journal;

/// <summary>
/// Summary of one recorded operation run in the journal, for maintenance purposes (e.g. deciding
/// whether it's old enough to prune) without loading every entry's full detail.
/// </summary>
/// <param name="RunId">Identifier of the run.</param>
/// <param name="LastActivityAtUtc">Timestamp of the most recent entry recorded for this run.</param>
/// <param name="EntryCount">Number of entries recorded for this run.</param>
public sealed record JournalRunSummary(Guid RunId, DateTimeOffset LastActivityAtUtc, int EntryCount);
