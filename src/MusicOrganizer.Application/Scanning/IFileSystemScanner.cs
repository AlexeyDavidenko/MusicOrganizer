namespace MusicOrganizer.Application.Scanning;

/// <summary>
/// Port for discovering audio files under a root folder. Implementations must stream results
/// lazily rather than materializing the whole collection, since it may contain millions of files.
/// </summary>
public interface IFileSystemScanner
{
    /// <summary>
    /// Recursively enumerates audio file paths under <paramref name="rootPath"/>.
    /// </summary>
    /// <param name="rootPath">Root folder to scan.</param>
    /// <param name="cancellationToken">Token used to stop enumeration early.</param>
    public IAsyncEnumerable<string> EnumerateAudioFilesAsync(string rootPath, CancellationToken cancellationToken = default);
}
