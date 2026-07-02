using MusicOrganizer.Domain;

namespace MusicOrganizer.Application.Scanning;

/// <summary>
/// Port for reading tags from a single audio file. Implementations must never throw for a
/// corrupted or unsupported file — such cases are reported as a failed <see cref="ScanEntry"/>
/// so that scanning the rest of a large collection can continue.
/// </summary>
public interface IAudioTagReader
{
    /// <summary>
    /// Reads the tags of the file at <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">Path of the file to read.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    public Task<ScanEntry> ReadTagsAsync(string filePath, CancellationToken cancellationToken = default);
}
