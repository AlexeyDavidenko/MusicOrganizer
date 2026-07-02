using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Application.Renaming;

/// <summary>
/// Port for renaming a single file on disk. Implementations must never throw for an expected
/// failure (locked file, permissions) — such cases are reported as a failed
/// <see cref="RenameResult"/> so that processing the rest of a large collection can continue.
/// </summary>
public interface IFileRenamer
{
    /// <summary>
    /// Checks whether a file already exists at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <param name="cancellationToken">Token used to cancel the check.</param>
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames the file at <paramref name="originalPath"/> to <paramref name="newPath"/>.
    /// </summary>
    /// <param name="originalPath">Current path of the file.</param>
    /// <param name="newPath">Path to rename the file to.</param>
    /// <param name="cancellationToken">Token used to cancel the rename.</param>
    public Task<RenameResult> RenameAsync(string originalPath, string newPath, CancellationToken cancellationToken = default);
}
