namespace MusicOrganizer.Domain.Deduplication;

/// <summary>
/// A set of files identified as duplicates of each other.
/// </summary>
/// <param name="Kind">How confidently these files were matched.</param>
/// <param name="Key">Internal grouping key (content hash for <see cref="DuplicateMatchKind.Exact"/>,
/// normalized Artist/Title for <see cref="DuplicateMatchKind.TagMatch"/>) — not meant for display.</param>
/// <param name="FilePaths">Paths of the files in this group.</param>
/// <param name="FileSize">
/// Size shared by every file in the group, when known. Always populated for
/// <see cref="DuplicateMatchKind.Exact"/> groups (identical content implies identical size);
/// <see langword="null"/> for <see cref="DuplicateMatchKind.TagMatch"/> groups, whose members can
/// differ in size.
/// </param>
public sealed record DuplicateGroup(
    DuplicateMatchKind Kind,
    string Key,
    IReadOnlyList<string> FilePaths,
    long? FileSize);
