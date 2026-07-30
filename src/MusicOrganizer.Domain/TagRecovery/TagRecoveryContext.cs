namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Precomputed, run-wide context made available to every <see cref="ITagRecoverySource"/> in the
/// chain. Sources that don't need it simply ignore it.
/// </summary>
/// <param name="FolderStatistics">Consensus Artist/Album per folder (see
/// <see cref="FolderTagStatistics"/>), keyed by normalized absolute directory path.</param>
public sealed record TagRecoveryContext(IReadOnlyDictionary<string, FolderTagStatistics> FolderStatistics);
