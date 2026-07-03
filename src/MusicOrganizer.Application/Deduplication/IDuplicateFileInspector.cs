namespace MusicOrganizer.Application.Deduplication;

/// <summary>
/// Port for the two cheap-to-expensive signals used to detect exact duplicates: file size (cheap,
/// used to skip hashing files that are provably unique) and content hash (expensive, only
/// computed for files that share a size with at least one other file).
/// </summary>
public interface IDuplicateFileInspector
{
    /// <summary>
    /// Gets the size, in bytes, of the file at <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">Path of the file to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task<long> GetSizeAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes a content hash for the file at <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">Path of the file to hash.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default);
}
