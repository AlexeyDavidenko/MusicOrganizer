namespace MusicOrganizer.Domain.Renaming;

/// <summary>
/// Outcome of actually renaming a single file on disk. A failure here never represents a thrown
/// exception — renaming must never abort processing of the rest of a collection.
/// </summary>
public sealed record RenameResult
{
    private RenameResult(string originalPath, string? newPath, string? error)
    {
        OriginalPath = originalPath;
        NewPath = newPath;
        Error = error;
    }

    /// <summary>Path of the file before the rename.</summary>
    public string OriginalPath { get; }

    /// <summary>Path of the file after the rename, when it succeeded.</summary>
    public string? NewPath { get; }

    /// <summary>Reason the rename failed, when it did.</summary>
    public string? Error { get; }

    /// <summary>True when the rename completed without error.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Creates a successful rename outcome.</summary>
    public static RenameResult Success(string originalPath, string newPath) => new(originalPath, newPath, null);

    /// <summary>Creates a failed rename outcome.</summary>
    public static RenameResult Failure(string originalPath, string error) => new(originalPath, null, error);
}
