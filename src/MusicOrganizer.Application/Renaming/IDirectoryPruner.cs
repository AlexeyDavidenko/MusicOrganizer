namespace MusicOrganizer.Application.Renaming;

/// <summary>
/// Port for removing a directory only if it's empty. Used by <see cref="OrganizeEngine"/> to
/// clean up folders left behind after all their files were moved elsewhere.
/// </summary>
public interface IDirectoryPruner
{
    /// <summary>
    /// Removes <paramref name="directory"/> if (and only if) it contains no files or
    /// subdirectories. Never throws for an expected failure (directory not empty, permissions,
    /// already gone) - such cases are reported as <see langword="false"/>.
    /// </summary>
    /// <param name="directory">Path of the directory to remove if empty.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><see langword="true"/> when the directory was removed.</returns>
    public Task<bool> TryRemoveIfEmptyAsync(string directory, CancellationToken cancellationToken = default);
}
