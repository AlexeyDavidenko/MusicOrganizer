namespace MusicOrganizer.Domain;

/// <summary>
/// Outcome of scanning a single file: either its tags were read successfully, or an error was
/// recorded. A failure here never represents a thrown exception — a corrupted or unsupported file
/// must not abort a scan of the rest of the collection.
/// </summary>
public sealed record ScanEntry
{
    private ScanEntry(string filePath, AudioTags? tags, string? error)
    {
        FilePath = filePath;
        Tags = tags;
        Error = error;
    }

    /// <summary>Absolute path of the scanned file.</summary>
    public string FilePath { get; }

    /// <summary>Tags read from the file, when scanning succeeded.</summary>
    public AudioTags? Tags { get; }

    /// <summary>Human-readable reason scanning failed, when it did.</summary>
    public string? Error { get; }

    /// <summary>True when the file was read without error.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Creates a successful scan outcome.</summary>
    public static ScanEntry Success(string filePath, AudioTags tags) => new(filePath, tags, null);

    /// <summary>Creates a failed scan outcome.</summary>
    public static ScanEntry Failure(string filePath, string error) => new(filePath, null, error);
}
