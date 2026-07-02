namespace MusicOrganizer.Domain.Renaming;

/// <summary>
/// Outcome of processing a single file for renaming.
/// </summary>
public sealed record RenameOutcome
{
    /// <summary>Path of the file as found.</summary>
    public required string OriginalPath { get; init; }

    /// <summary>
    /// Target path proposed by the rename template, or <see langword="null"/> when Artist/Title
    /// were missing and no name could be built.
    /// </summary>
    public string? ProposedPath { get; init; }

    /// <summary>True when the file was actually renamed on disk.</summary>
    public bool Applied { get; init; }

    /// <summary>Reason processing failed, when it did.</summary>
    public string? Error { get; init; }

    /// <summary>True when a proposed name was built and it differs from the current name.</summary>
    public bool NeedsRename => ProposedPath is not null
        && !string.Equals(OriginalPath, ProposedPath, StringComparison.Ordinal);
}
