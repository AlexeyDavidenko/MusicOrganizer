namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Proposes filling in missing Artist/Title/TrackNumber tags from a file's name using
/// <see cref="HeuristicFilenameParser"/> (TAG RECOVERY level 7, Heuristics). Never overwrites a
/// field that is already present.
/// </summary>
public static class HeuristicFilenameTagRecovery
{
    /// <summary>
    /// Proposes a tag recovery for a file based on its name.
    /// </summary>
    /// <param name="fileNameWithoutExtension">File name without its extension.</param>
    /// <param name="current">Tags currently present on the file.</param>
    /// <returns>A proposal with any recovered fields, or unchanged tags if none applied.</returns>
    public static TagRecoveryProposal Propose(string fileNameWithoutExtension, AudioTags current)
    {
        var candidate = HeuristicFilenameParser.TryParse(fileNameWithoutExtension);
        var trackNumberCandidate = HeuristicFilenameParser.TryParseTrackNumber(fileNameWithoutExtension);

        var recoveredFields = new List<string>();
        var artist = current.Artist;
        var title = current.Title;
        var trackNumber = current.TrackNumber;

        if (current.Artist is null && candidate is not null)
        {
            artist = candidate.Value.Artist;
            recoveredFields.Add(nameof(AudioTags.Artist));
        }

        if (current.Title is null && candidate is not null)
        {
            title = candidate.Value.Title;
            recoveredFields.Add(nameof(AudioTags.Title));
        }

        if (current.TrackNumber is null && trackNumberCandidate is not null)
        {
            trackNumber = trackNumberCandidate;
            recoveredFields.Add(nameof(AudioTags.TrackNumber));
        }

        if (recoveredFields.Count == 0)
        {
            return new TagRecoveryProposal(current, []);
        }

        var merged = current with { Artist = artist, Title = title, TrackNumber = trackNumber };
        return new TagRecoveryProposal(merged, recoveredFields);
    }
}
