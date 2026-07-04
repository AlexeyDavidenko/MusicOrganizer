namespace MusicOrganizer.Domain.Deduplication;

/// <summary>
/// Outcome of processing a single duplicate file for removal. Only emitted for files that are
/// *not* the one being kept in their group.
/// </summary>
public sealed record DuplicateRemovalOutcome
{
    /// <summary>Path of the duplicate file being processed.</summary>
    public required string FilePath { get; init; }

    /// <summary>Path of the file being kept in this duplicate group.</summary>
    public required string KeptFilePath { get; init; }

    /// <summary>True when the file was actually deleted from disk.</summary>
    public bool Applied { get; init; }

    /// <summary>Reason processing failed, when it did.</summary>
    public string? Error { get; init; }
}
