using System.Text.RegularExpressions;

namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Parses filenames the strict "Artist - Title" rule (<see cref="FilenameArtistTitleParser"/>)
/// rejects: leading track-number prefixes, trailing noise like "(Live)"/"[Remastered]", and the
/// project's own underscore-for-space rename convention (see ADR-0003) reversed on read. Returns
/// <see langword="null"/> whenever the result would still be ambiguous, since an unconfident guess
/// must never be applied automatically.
/// </summary>
public static partial class HeuristicFilenameParser
{
    private static readonly string[] Separators = [" - ", "-"];

    /// <summary>
    /// Attempts to parse <paramref name="fileNameWithoutExtension"/> into an Artist and Title.
    /// </summary>
    /// <param name="fileNameWithoutExtension">File name without its extension.</param>
    public static (string Artist, string Title)? TryParse(string fileNameWithoutExtension)
    {
        var cleaned = StripTrackNumberPrefix(StripTrailingNoise(fileNameWithoutExtension));

        foreach (var separator in Separators)
        {
            var parts = cleaned.Split(separator);
            if (parts.Length != 2)
            {
                continue;
            }

            var artist = parts[0].Trim();
            var title = parts[1].Trim();

            if (!IsConfidentSegment(artist) || !IsConfidentSegment(title))
            {
                return null;
            }

            return (Detag(artist), Detag(title));
        }

        return null;
    }

    private static string StripTrailingNoise(string value)
    {
        var result = value;
        while (TrailingNoiseRegex().IsMatch(result))
        {
            result = TrailingNoiseRegex().Replace(result, string.Empty);
        }

        return result;
    }

    private static string StripTrackNumberPrefix(string value) =>
        TrackNumberPrefixRegex().Replace(value, string.Empty);

    private static string Detag(string segment) => segment.Replace('_', ' ').Trim();

    private static bool IsConfidentSegment(string segment) =>
        segment.Length > 0 && !segment.All(char.IsDigit);

    [GeneratedRegex(@"\s*[\(\[][^()\[\]]*[\)\]]\s*$")]
    private static partial Regex TrailingNoiseRegex();

    [GeneratedRegex(@"^\d{1,3}[\s._-]+")]
    private static partial Regex TrackNumberPrefixRegex();
}
