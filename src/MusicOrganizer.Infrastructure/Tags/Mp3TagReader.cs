using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;

namespace MusicOrganizer.Infrastructure.Tags;

/// <summary>
/// Reads ID3 tags from MP3 files using TagLibSharp. Corrupted or unsupported files are reported
/// as a failed <see cref="ScanEntry"/> instead of throwing, so scanning a large collection can
/// continue past a single bad file.
/// </summary>
public sealed partial class Mp3TagReader : IAudioTagReader
{
    private readonly ILogger<Mp3TagReader> _logger;

    /// <summary>
    /// Creates a new <see cref="Mp3TagReader"/>.
    /// </summary>
    public Mp3TagReader(ILogger<Mp3TagReader> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<ScanEntry> ReadTagsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var file = TagLib.File.Create(filePath);
            var tags = MapTags(file.Tag);
            return Task.FromResult(ScanEntry.Success(filePath, tags));
        }
        catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException
            or IOException or UnauthorizedAccessException)
        {
            LogTagReadFailed(filePath, ex);
            return Task.FromResult(ScanEntry.Failure(filePath, ex.Message));
        }
    }

    private static AudioTags MapTags(TagLib.Tag tag) => new(
        Title: string.IsNullOrWhiteSpace(tag.Title) ? null : tag.Title,
        Artist: string.IsNullOrWhiteSpace(tag.JoinedPerformers) ? null : tag.JoinedPerformers,
        Album: string.IsNullOrWhiteSpace(tag.Album) ? null : tag.Album,
        Year: tag.Year == 0 ? null : (int)tag.Year,
        TrackNumber: tag.Track == 0 ? null : (int)tag.Track,
        Genre: string.IsNullOrWhiteSpace(tag.JoinedGenres) ? null : tag.JoinedGenres);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to read tags from {FilePath}")]
    private partial void LogTagReadFailed(string filePath, Exception exception);
}
