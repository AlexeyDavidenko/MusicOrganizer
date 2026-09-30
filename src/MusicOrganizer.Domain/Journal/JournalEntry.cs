namespace MusicOrganizer.Domain.Journal;

/// <summary>
/// A crash-safe restore point recorded before a file (or empty directory) was mutated. Valid to
/// restore regardless of whether the mutation that followed later succeeded or failed. Exactly
/// one of <paramref name="BackupPath"/> (an in-place content change, e.g. a tag write) or
/// <paramref name="NewPath"/> (a path change, e.g. a rename) is set for a file entry; when
/// <paramref name="OriginalPath"/> is a directory that was removed (always empty when removed -
/// nothing to back up), both are <see langword="null"/> and restoring simply recreates it.
/// </summary>
/// <param name="Id">Unique identifier of this entry.</param>
/// <param name="RunId">Identifier of the operation run this entry belongs to.</param>
/// <param name="OriginalPath">Path of the file or directory before the operation.</param>
/// <param name="BackupPath">Path of the pre-mutation byte copy, for in-place content changes.</param>
/// <param name="NewPath">Path the file was moved to, for path-changing operations.</param>
/// <param name="OperationType">Name of the operation that recorded this entry (e.g. "tag-recovery", "rename").</param>
/// <param name="TimestampUtc">When the entry was recorded.</param>
public sealed record JournalEntry(
    Guid Id,
    Guid RunId,
    string OriginalPath,
    string? BackupPath,
    string? NewPath,
    string OperationType,
    DateTimeOffset TimestampUtc);
