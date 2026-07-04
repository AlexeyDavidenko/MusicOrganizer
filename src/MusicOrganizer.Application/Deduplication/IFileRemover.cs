using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Application.Deduplication;

/// <summary>
/// Port for deleting a single file. Implementations must never throw for an expected failure
/// (locked file, permissions) — such cases are reported as a failed <see cref="FileRemovalResult"/>
/// so that processing the rest of a large collection can continue.
/// </summary>
public interface IFileRemover
{
    /// <summary>
    /// Deletes the file at <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">Path of the file to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task<FileRemovalResult> DeleteAsync(string filePath, CancellationToken cancellationToken = default);
}
