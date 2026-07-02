namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Outcome of writing tags to a single file: either it succeeded, or an error was recorded. A
/// failure here never represents a thrown exception — writing must never abort processing of the
/// rest of a collection.
/// </summary>
public sealed record TagWriteResult
{
    private TagWriteResult(string filePath, string? error)
    {
        FilePath = filePath;
        Error = error;
    }

    /// <summary>Path of the file that was written to.</summary>
    public string FilePath { get; }

    /// <summary>Reason the write failed, when it did.</summary>
    public string? Error { get; }

    /// <summary>True when the write completed without error.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Creates a successful write outcome.</summary>
    public static TagWriteResult Success(string filePath) => new(filePath, null);

    /// <summary>Creates a failed write outcome.</summary>
    public static TagWriteResult Failure(string filePath, string error) => new(filePath, error);
}
