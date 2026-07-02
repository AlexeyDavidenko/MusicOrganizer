namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Proposes filling in missing Artist/Title tags from a file's name. Never overwrites a field
/// that is already present — recovery only fills gaps, it never corrects existing data.
/// </summary>
public static class FilenameTagRecovery
{
    /// <summary>
    /// Proposes a tag recovery for a file based on its name.
    /// </summary>
    /// <param name="fileNameWithoutExtension">File name without its extension.</param>
    /// <param name="current">Tags currently present on the file.</param>
    public static TagRecoveryProposal Propose(string fileNameWithoutExtension, AudioTags current)
    {
        var candidate = FilenameArtistTitleParser.TryParse(fileNameWithoutExtension);
        if (candidate is null)
        {
            return new TagRecoveryProposal(current, []);
        }

        var recoveredFields = new List<string>();
        var artist = current.Artist;
        var title = current.Title;

        if (current.Artist is null)
        {
            artist = candidate.Value.Artist;
            recoveredFields.Add(nameof(AudioTags.Artist));
        }

        if (current.Title is null)
        {
            title = candidate.Value.Title;
            recoveredFields.Add(nameof(AudioTags.Title));
        }

        if (recoveredFields.Count == 0)
        {
            return new TagRecoveryProposal(current, []);
        }

        var merged = current with { Artist = artist, Title = title };
        return new TagRecoveryProposal(merged, recoveredFields);
    }
}
