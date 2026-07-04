namespace MusicOrganizer.Domain.Deduplication;

/// <summary>
/// Decides which file in a group of duplicates should be kept when the rest are removed.
/// </summary>
public static class DuplicateKeeperSelector
{
    /// <summary>
    /// Selects the file to keep: the one with the shortest path, ties broken by ordinal string
    /// comparison for a deterministic result.
    /// </summary>
    /// <param name="filePaths">Paths of the files in a duplicate group.</param>
    public static string SelectKeeper(IReadOnlyList<string> filePaths) =>
        filePaths
            .OrderBy(path => path.Length)
            .ThenBy(path => path, StringComparer.Ordinal)
            .First();
}
