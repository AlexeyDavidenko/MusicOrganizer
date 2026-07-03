namespace MusicOrganizer.Domain.Deduplication;

/// <summary>
/// Builds the grouping key used to detect probable duplicates by matching Artist/Title tags,
/// tolerant of case and surrounding whitespace differences.
/// </summary>
public static class TagMatchKey
{
    private const char Separator = '\u001F';

    /// <summary>
    /// Builds a normalized key for the given Artist and Title.
    /// </summary>
    /// <param name="artist">Artist tag value.</param>
    /// <param name="title">Title tag value.</param>
    public static string Build(string artist, string title) =>
        $"{Normalize(artist)}{Separator}{Normalize(title)}";

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
