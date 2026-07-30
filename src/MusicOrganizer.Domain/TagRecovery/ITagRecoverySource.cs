namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// A single source of tag recovery in the priority chain (see PROMPT.md, "TAG RECOVERY"). Pure —
/// no IO. Implementations must only fill fields that are currently missing, never overwrite an
/// existing value.
/// </summary>
public interface ITagRecoverySource
{
    /// <summary>
    /// Proposes filling in missing tags for a file, based on whatever signal this source uses.
    /// </summary>
    /// <param name="filePath">Path of the file being processed.</param>
    /// <param name="rootPath">Root folder the current scan/recovery run started from.</param>
    /// <param name="current">Tags as currently known (possibly already partially recovered by an
    /// earlier, higher-priority source in the chain).</param>
    /// <param name="context">Precomputed, run-wide context (e.g. folder statistics). Sources that
    /// don't need it ignore it.</param>
    public TagRecoveryProposal Propose(string filePath, string rootPath, AudioTags current, TagRecoveryContext context);
}
