using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Application.Renaming;

/// <summary>
/// Resolves a proposed target path against the real file system, appending " (N)" until a free
/// name is found. Shared by any use case that relocates a file to a tag-derived path
/// (<see cref="RenameEngine"/>, <see cref="OrganizeEngine"/>) so the collision-handling loop isn't
/// duplicated between them.
/// </summary>
internal static class FileRelocationConflictResolver
{
    private const int MaxConflictAttempts = 999;

    /// <summary>
    /// Finds a free path for <paramref name="proposedPath"/>, or the path unchanged if it's
    /// already the file's current path.
    /// </summary>
    /// <param name="fileRenamer">Port used to check whether a candidate path already exists.</param>
    /// <param name="proposedPath">Target path proposed by the caller's naming/organization template.</param>
    /// <param name="originalPath">Current path of the file being relocated.</param>
    /// <param name="cancellationToken">Token used to cancel the resolution.</param>
    /// <returns>The free path to use, or an error if none could be found.</returns>
    public static async Task<(string? ResolvedPath, string? Error)> ResolveAsync(
        IFileRenamer fileRenamer,
        string proposedPath,
        string originalPath,
        CancellationToken cancellationToken)
    {
        if (string.Equals(proposedPath, originalPath, StringComparison.Ordinal))
        {
            return (proposedPath, null);
        }

        var candidate = proposedPath;
        for (var attempt = 1; attempt <= MaxConflictAttempts; attempt++)
        {
            if (!await fileRenamer.ExistsAsync(candidate, cancellationToken))
            {
                return (candidate, null);
            }

            candidate = RenameConflictResolver.NextCandidate(proposedPath, attempt);
        }

        return (null, $"Could not find a free name for {proposedPath} after {MaxConflictAttempts} attempts");
    }
}
