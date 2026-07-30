namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Outcome of attempting tag recovery on a single file.
/// </summary>
public sealed record TagRecoveryOutcome
{
    /// <summary>Path of the file that was processed.</summary>
    public required string FilePath { get; init; }

    /// <summary>Names of the fields that were (or, in a dry run, would be) recovered.</summary>
    public IReadOnlyList<string> RecoveredFields { get; init; } = [];

    /// <summary>True when the recovered tags were actually written to the file.</summary>
    public bool Applied { get; init; }

    /// <summary>Reason processing failed, when it did.</summary>
    public string? Error { get; init; }

    /// <summary>
    /// True when, after every source in the chain has run, the file is still missing Artist or
    /// Title (TAG RECOVERY level 8, Manual Review) — reporting only, never set when
    /// <see cref="Error"/> is set.
    /// </summary>
    public bool NeedsManualReview { get; init; }

    /// <summary>True when at least one field was recovered (or would be, in a dry run).</summary>
    public bool HasRecovery => RecoveredFields.Count > 0;
}
