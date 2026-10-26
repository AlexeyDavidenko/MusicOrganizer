namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Consensus Artist/Album/Year/Genre values for a single folder, derived from sibling files that
/// already carry a value for that field. Null when there is no consensus (no agreeing siblings, or
/// siblings disagree). TrackNumber is deliberately absent — it differs per track by definition.
/// </summary>
/// <param name="Artist">Consensus Artist for the folder, if any.</param>
/// <param name="Album">Consensus Album for the folder, if any.</param>
/// <param name="Year">Consensus Year for the folder, if any.</param>
/// <param name="Genre">Consensus Genre for the folder, if any.</param>
public sealed record FolderTagStatistics(string? Artist, string? Album, int? Year, string? Genre);
