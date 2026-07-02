namespace MusicOrganizer.Domain.Renaming;

/// <summary>
/// Generates alternative candidate paths when a proposed rename target already exists. Pure
/// pattern generation only — checking whether a candidate actually exists is an IO concern that
/// belongs to the caller.
/// </summary>
public static class RenameConflictResolver
{
    /// <summary>
    /// Builds the Nth alternative candidate for <paramref name="path"/>, e.g. attempt 1 of
    /// "Artist-Title.mp3" is "Artist-Title (1).mp3".
    /// </summary>
    /// <param name="path">Original proposed path.</param>
    /// <param name="attempt">1-based attempt number.</param>
    public static string NextCandidate(string path, int attempt)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var fileName = $"{stem} ({attempt}){extension}";
        return directory.Length == 0 ? fileName : Path.Combine(directory, fileName);
    }
}
