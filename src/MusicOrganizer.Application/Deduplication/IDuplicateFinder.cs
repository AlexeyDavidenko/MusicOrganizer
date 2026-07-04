using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Application.Deduplication;

/// <summary>
/// Finds duplicate audio files under a root folder. Extracted purely so consumers (like
/// <see cref="DuplicateRemovalService"/>) can be unit-tested against a fake instead of the real
/// <see cref="DuplicateFinder"/> and its own three dependencies.
/// </summary>
public interface IDuplicateFinder
{
    /// <summary>
    /// Recursively scans <paramref name="rootPath"/> and returns every group of duplicate files
    /// found, both exact-content and tag-match groups.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="cancellationToken">Token used to stop the scan early.</param>
    public Task<IReadOnlyList<DuplicateGroup>> FindAsync(string rootPath, CancellationToken cancellationToken = default);
}
