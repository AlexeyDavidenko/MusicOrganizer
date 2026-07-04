namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Adapts <see cref="FilenameTagRecovery"/> to <see cref="ITagRecoverySource"/> (TAG RECOVERY
/// priority level 2, Filename).
/// </summary>
public sealed class FilenameTagRecoverySource : ITagRecoverySource
{
    /// <inheritdoc />
    public TagRecoveryProposal Propose(string filePath, string rootPath, AudioTags current) =>
        FilenameTagRecovery.Propose(Path.GetFileNameWithoutExtension(filePath), current);
}
