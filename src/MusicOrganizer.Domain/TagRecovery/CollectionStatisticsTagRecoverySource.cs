namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Adapts <see cref="CollectionStatisticsTagRecovery"/> to <see cref="ITagRecoverySource"/> (TAG
/// RECOVERY priority level 6, Existing Collection Statistics).
/// </summary>
public sealed class CollectionStatisticsTagRecoverySource : ITagRecoverySource
{
    /// <inheritdoc />
    public TagRecoveryProposal Propose(string filePath, string rootPath, AudioTags current, TagRecoveryContext context)
    {
        var folder = Path.GetDirectoryName(filePath);
        var statistics = folder is not null && context.FolderStatistics.TryGetValue(folder, out var found)
            ? found
            : null;

        return CollectionStatisticsTagRecovery.Propose(current, statistics);
    }
}
