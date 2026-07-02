namespace MusicOrganizer.Domain.Renaming;

/// <summary>
/// Builds a file name using the project's default rename template: "Artist-Title.mp3", with
/// spaces inside each component replaced by underscores rather than removed. See PROMPT.md,
/// "FILE RENAMING".
/// </summary>
public static class DefaultRenameTemplate
{
    /// <summary>
    /// Builds the target file name for a track with the given Artist and Title.
    /// </summary>
    /// <param name="artist">Artist tag value.</param>
    /// <param name="title">Title tag value.</param>
    public static string BuildFileName(string artist, string title)
    {
        var sanitizedArtist = FileNameSanitizer.SanitizeComponent(artist);
        var sanitizedTitle = FileNameSanitizer.SanitizeComponent(title);
        return $"{sanitizedArtist}-{sanitizedTitle}.mp3";
    }
}
