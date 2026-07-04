namespace MusicOrganizer.Domain.Deduplication;

/// <summary>
/// Outcome of deleting a single file. A failure here never represents a thrown exception —
/// removal must never abort processing of the rest of a collection.
/// </summary>
public sealed record FileRemovalResult
{
    private FileRemovalResult(string filePath, string? error)
    {
        FilePath = filePath;
        Error = error;
    }

    /// <summary>Path of the file that was deleted.</summary>
    public string FilePath { get; }

    /// <summary>Reason the deletion failed, when it did.</summary>
    public string? Error { get; }

    /// <summary>True when the deletion completed without error.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Creates a successful removal outcome.</summary>
    public static FileRemovalResult Success(string filePath) => new(filePath, null);

    /// <summary>Creates a failed removal outcome.</summary>
    public static FileRemovalResult Failure(string filePath, string error) => new(filePath, error);
}
