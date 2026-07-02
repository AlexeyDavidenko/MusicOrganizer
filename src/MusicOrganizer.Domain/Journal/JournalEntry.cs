namespace MusicOrganizer.Domain.Journal;

/// <summary>
/// A crash-safe restore point recorded before a file was mutated. Valid to restore regardless of
/// whether the mutation that followed later succeeded or failed.
/// </summary>
/// <param name="Id">Unique identifier of this entry.</param>
/// <param name="RunId">Identifier of the operation run this entry belongs to.</param>
/// <param name="OriginalPath">Path of the file that was backed up.</param>
/// <param name="BackupPath">Path where the pre-mutation copy of the file was stored.</param>
/// <param name="OperationType">Name of the operation that recorded this entry (e.g. "tag-recovery").</param>
/// <param name="TimestampUtc">When the backup was made.</param>
public sealed record JournalEntry(
    Guid Id,
    Guid RunId,
    string OriginalPath,
    string BackupPath,
    string OperationType,
    DateTimeOffset TimestampUtc);
