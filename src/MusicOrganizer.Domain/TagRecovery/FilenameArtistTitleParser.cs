namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Parses an "Artist - Title" style filename into candidate Artist/Title values. Returns
/// <see langword="null"/> whenever the filename is ambiguous, since an unconfident guess must
/// never be applied automatically.
/// </summary>
public static class FilenameArtistTitleParser
{
    private static readonly string[] Separators = [" - ", "-"];

    /// <summary>
    /// Attempts to parse <paramref name="fileNameWithoutExtension"/> into an Artist and Title.
    /// </summary>
    /// <param name="fileNameWithoutExtension">File name without its extension.</param>
    public static (string Artist, string Title)? TryParse(string fileNameWithoutExtension)
    {
        foreach (var separator in Separators)
        {
            var parts = fileNameWithoutExtension.Split(separator);
            if (parts.Length != 2)
            {
                continue;
            }

            var artist = parts[0].Trim();
            var title = parts[1].Trim();

            if (IsConfidentSegment(artist) && IsConfidentSegment(title))
            {
                return (artist, title);
            }

            return null;
        }

        return null;
    }

    private static bool IsConfidentSegment(string segment) =>
        segment.Length > 0 && !segment.All(char.IsDigit);
}
