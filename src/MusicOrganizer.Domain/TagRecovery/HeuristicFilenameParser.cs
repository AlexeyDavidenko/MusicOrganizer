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
    /// <returns>The parsed Artist/Title pair, or <see langword="null"/> if still ambiguous.</returns>
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

    /// <summary>
    /// Attempts to read a leading track number from <paramref name="fileNameWithoutExtension"/>
    /// (<c>"01 - Title"</c>, <c>"01. Title"</c>, <c>"01_Title"</c>). Stricter than the prefix
    /// stripped by <see cref="TryParse"/>: the number must be followed by an explicit
    /// <c>.</c>/<c>_</c>/<c>-</c> delimiter, so a plain space (<c>"50 Cent - In Da Club"</c>) is
    /// never read as a track number.
    /// </summary>
    /// <param name="fileNameWithoutExtension">File name without its extension.</param>
    /// <returns>The track number, or <see langword="null"/> if there's no confident prefix.</returns>
    public static int? TryParseTrackNumber(string fileNameWithoutExtension)
    {
        var match = DelimitedTrackNumberPrefixRegex().Match(fileNameWithoutExtension);
        if (!match.Success)
        {
            return null;
        }

        var remainder = fileNameWithoutExtension[match.Length..].Trim();
        if (!IsConfidentSegment(remainder))
        {
            return null;
        }

        var trackNumber = int.Parse(match.Groups["track"].Value, System.Globalization.CultureInfo.InvariantCulture);
        return trackNumber > 0 ? trackNumber : null;
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

    [GeneratedRegex(@"^(?<track>\d{1,3})\s*[._-]\s*")]
    private static partial Regex DelimitedTrackNumberPrefixRegex();
}
