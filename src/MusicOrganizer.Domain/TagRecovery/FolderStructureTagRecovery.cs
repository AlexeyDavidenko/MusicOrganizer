namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Proposes filling in missing Artist/Album tags from the folder structure a file was found in,
/// relative to the root folder a scan started from. Covers PROMPT.md's "Album Folder"/"Artist
/// Folder"/"Parent Folder" TAG RECOVERY levels as one depth-aware rule rather than three
/// independent ones, since they aren't actually independent: a flat, single-level folder can't be
/// both an "Album Folder" and something else at the same time.
/// </summary>
/// <remarks>
/// Three cases, based on how many folder levels separate the file from <c>rootPath</c>:
/// two or more (<c>Root/Artist/Album/track.mp3</c>) — the immediate parent is Album, the
/// grandparent is Artist; exactly one (<c>Root/Artist/track.mp3</c>, no album subfolder) — that
/// single folder is Artist only ("Parent Folder" in PROMPT.md's list — the flat-collection
/// fallback); zero (file directly under <c>rootPath</c>) — no usable folder signal at all.
/// </remarks>
public static class FolderStructureTagRecovery
{
    /// <summary>
    /// Proposes a tag recovery for a file based on its folder structure.
    /// </summary>
    /// <param name="filePath">Path of the file being processed.</param>
    /// <param name="rootPath">Root folder the current scan/recovery run started from.</param>
    /// <param name="current">Tags currently present on the file.</param>
    public static TagRecoveryProposal Propose(string filePath, string rootPath, AudioTags current)
    {
        var normalizedRoot = NormalizePath(rootPath);

        var parentDir = Path.GetDirectoryName(filePath);
        if (parentDir is null || PathsEqual(parentDir, normalizedRoot))
        {
            return new TagRecoveryProposal(current, []);
        }

        var grandparentDir = Path.GetDirectoryName(parentDir);
        var recoveredFields = new List<string>();
        var artist = current.Artist;
        var album = current.Album;

        if (grandparentDir is null || PathsEqual(grandparentDir, normalizedRoot))
        {
            // Exactly one level under the root: flat collection, the single folder is Artist.
            if (artist is null)
            {
                artist = Path.GetFileName(parentDir);
                recoveredFields.Add(nameof(AudioTags.Artist));
            }
        }
        else
        {
            // Two or more levels under the root: nested Artist/Album convention.
            if (album is null)
            {
                album = Path.GetFileName(parentDir);
                recoveredFields.Add(nameof(AudioTags.Album));
            }

            if (artist is null)
            {
                artist = Path.GetFileName(grandparentDir);
                recoveredFields.Add(nameof(AudioTags.Artist));
            }
        }

        if (recoveredFields.Count == 0)
        {
            return new TagRecoveryProposal(current, []);
        }

        var merged = current with { Artist = artist, Album = album };
        return new TagRecoveryProposal(merged, recoveredFields);
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static bool PathsEqual(string path, string normalizedOther) =>
        string.Equals(NormalizePath(path), normalizedOther, StringComparison.OrdinalIgnoreCase);
}
