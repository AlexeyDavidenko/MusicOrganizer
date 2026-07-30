namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Adapts <see cref="HeuristicFilenameTagRecovery"/> to <see cref="ITagRecoverySource"/> (TAG
/// RECOVERY priority level 7, Heuristics).
/// </summary>
public sealed class HeuristicFilenameTagRecoverySource : ITagRecoverySource
{
    /// <inheritdoc />
    public TagRecoveryProposal Propose(string filePath, string rootPath, AudioTags current, TagRecoveryContext context) =>
        HeuristicFilenameTagRecovery.Propose(Path.GetFileNameWithoutExtension(filePath), current);
}
