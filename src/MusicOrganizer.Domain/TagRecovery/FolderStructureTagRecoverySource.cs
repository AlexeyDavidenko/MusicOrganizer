namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Adapts <see cref="FolderStructureTagRecovery"/> to <see cref="ITagRecoverySource"/> (TAG
/// RECOVERY priority levels 3-5: Album Folder, Artist Folder, Parent Folder).
/// </summary>
public sealed class FolderStructureTagRecoverySource : ITagRecoverySource
{
    /// <inheritdoc />
    public TagRecoveryProposal Propose(string filePath, string rootPath, AudioTags current) =>
        FolderStructureTagRecovery.Propose(filePath, rootPath, current);
}
