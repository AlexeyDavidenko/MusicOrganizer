using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Infrastructure.Tags;

/// <summary>
/// Writes ID3 tags to MP3 files using TagLibSharp. Corrupted or unsupported files are reported as
/// a failed <see cref="TagWriteResult"/> instead of throwing, so processing a large collection can
/// continue past a single bad file.
/// </summary>
public sealed partial class Mp3TagWriter : ITagWriter
{
    private readonly ILogger<Mp3TagWriter> _logger;

    /// <summary>
    /// Creates a new <see cref="Mp3TagWriter"/>.
    /// </summary>
    public Mp3TagWriter(ILogger<Mp3TagWriter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<TagWriteResult> WriteTagsAsync(string filePath, AudioTags tags, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var file = TagLib.File.Create(filePath);
            ApplyTags(file.Tag, tags);
            file.Save();
            return Task.FromResult(TagWriteResult.Success(filePath));
        }
        catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException
            or IOException or UnauthorizedAccessException)
        {
            LogTagWriteFailed(filePath, ex);
            return Task.FromResult(TagWriteResult.Failure(filePath, ex.Message));
        }
    }

    private static void ApplyTags(TagLib.Tag tag, AudioTags tags)
    {
        if (tags.Title is not null)
        {
            tag.Title = tags.Title;
        }

        if (tags.Artist is not null)
        {
            tag.Performers = [tags.Artist];
        }

        if (tags.Album is not null)
        {
            tag.Album = tags.Album;
        }

        if (tags.Year is not null)
        {
            tag.Year = (uint)tags.Year.Value;
        }

        if (tags.TrackNumber is not null)
        {
            tag.Track = (uint)tags.TrackNumber.Value;
        }

        if (tags.Genre is not null)
        {
            tag.Genres = [tags.Genre];
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to write tags to {FilePath}")]
    private partial void LogTagWriteFailed(string filePath, Exception exception);
}
