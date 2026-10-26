namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Proposes filling in missing Artist/Album/Year/Genre tags from consensus among sibling files in the same
/// folder that already carry a value for that field (TAG RECOVERY level 6, "Existing Collection
/// Statistics" in PROMPT.md). Pure — no IO; <see cref="Build"/> takes tags the caller already read.
/// </summary>
public static class CollectionStatisticsTagRecovery
{
    /// <summary>
    /// Minimum number of siblings that must agree on a value before it's treated as consensus. A
    /// single already-tagged file next to many untagged ones is not a strong enough signal to
    /// force a folder-wide guess.
    /// </summary>
    private const int MinimumAgreeingSiblings = 2;

    /// <summary>
    /// Builds per-folder consensus statistics from every successfully-read file's tags.
    /// </summary>
    /// <param name="tagsByPath">Tags already read for every file in the current run, keyed by
    /// file path.</param>
    /// <returns>Per-folder consensus statistics.</returns>
    public static TagRecoveryContext Build(IReadOnlyDictionary<string, AudioTags> tagsByPath)
    {
        var byFolder = new Dictionary<string, List<AudioTags>>();
        foreach (var (path, tags) in tagsByPath)
        {
            var folder = Path.GetDirectoryName(path);
            if (folder is null)
            {
                continue;
            }

            if (!byFolder.TryGetValue(folder, out var list))
            {
                list = [];
                byFolder[folder] = list;
            }

            list.Add(tags);
        }

        var statistics = new Dictionary<string, FolderTagStatistics>();
        foreach (var (folder, tagsInFolder) in byFolder)
        {
            var artist = Consensus(tagsInFolder.Select(t => t.Artist));
            var album = Consensus(tagsInFolder.Select(t => t.Album));
            var year = Consensus(tagsInFolder.Select(t => t.Year));
            var genre = Consensus(tagsInFolder.Select(t => t.Genre));
            statistics[folder] = new FolderTagStatistics(artist, album, year, genre);
        }

        return new TagRecoveryContext(statistics);
    }

    /// <summary>
    /// Proposes a tag recovery for a file based on its folder's consensus statistics.
    /// </summary>
    /// <param name="current">Tags currently present on the file.</param>
    /// <param name="statistics">Consensus statistics for the file's folder, if any.</param>
    /// <returns>A proposal with any recovered fields, or unchanged tags if none applied.</returns>
    public static TagRecoveryProposal Propose(AudioTags current, FolderTagStatistics? statistics)
    {
        if (statistics is null)
        {
            return new TagRecoveryProposal(current, []);
        }

        var recoveredFields = new List<string>();
        var artist = current.Artist;
        var album = current.Album;
        var year = current.Year;
        var genre = current.Genre;

        if (artist is null && statistics.Artist is not null)
        {
            artist = statistics.Artist;
            recoveredFields.Add(nameof(AudioTags.Artist));
        }

        if (album is null && statistics.Album is not null)
        {
            album = statistics.Album;
            recoveredFields.Add(nameof(AudioTags.Album));
        }

        if (year is null && statistics.Year is not null)
        {
            year = statistics.Year;
            recoveredFields.Add(nameof(AudioTags.Year));
        }

        if (genre is null && statistics.Genre is not null)
        {
            genre = statistics.Genre;
            recoveredFields.Add(nameof(AudioTags.Genre));
        }

        if (recoveredFields.Count == 0)
        {
            return new TagRecoveryProposal(current, []);
        }

        var merged = current with { Artist = artist, Album = album, Year = year, Genre = genre };
        return new TagRecoveryProposal(merged, recoveredFields);
    }

    private static string? Consensus(IEnumerable<string?> values) =>
        AgreedValues(values.OfType<string>()) is [var value] ? value : null;

    private static int? Consensus(IEnumerable<int?> values) =>
        AgreedValues(values.OfType<int>()) is [var value] ? value : null;

    private static List<T> AgreedValues<T>(IEnumerable<T> nonNull)
    {
        var values = nonNull.ToList();
        return values.Count < MinimumAgreeingSiblings ? [] : values.Distinct().ToList();
    }
}
