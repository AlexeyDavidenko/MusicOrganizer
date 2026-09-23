namespace MusicOrganizer.Domain.Encoding;

/// <summary>
/// Outcome of processing a single file for encoding correction.
/// </summary>
public sealed record EncodingFixOutcome
{
    /// <summary>Path of the file as found.</summary>
    public required string FilePath { get; init; }

    /// <summary>Names of the fields a confident encoding correction was found for.</summary>
    public IReadOnlyList<string> FixedFields { get; init; } = [];

    /// <summary>True when the correction was actually written to disk.</summary>
    public bool Applied { get; init; }

    /// <summary>Reason processing failed, when it did.</summary>
    public string? Error { get; init; }

    /// <summary>
    /// True when a field looked suspicious (non-ASCII bytes) but no candidate encoding won
    /// confidently, so nothing was changed for it.
    /// </summary>
    public bool NeedsManualReview { get; init; }

    /// <summary>True when at least one field was corrected.</summary>
    public bool HasFix => FixedFields.Count > 0;
}
