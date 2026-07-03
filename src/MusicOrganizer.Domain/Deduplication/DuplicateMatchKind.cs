namespace MusicOrganizer.Domain.Deduplication;

/// <summary>
/// How confidently a group of files was identified as duplicates.
/// </summary>
public enum DuplicateMatchKind
{
    /// <summary>Files have byte-identical content.</summary>
    Exact,

    /// <summary>Files have matching Artist/Title tags but different content.</summary>
    TagMatch,
}
